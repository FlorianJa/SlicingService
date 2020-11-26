using GcodeToMesh.MeshClasses;
using GcodeToMesh.MeshDecimator;
using GcodeToMesh.MeshDecimator.Math;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GcodeToMesh
{
    public class GcodeAnalyser
    {

        public int layercluster = 1;
        private float plasticwidth = 0.4f;
        private ConcurrentQueue<MeshCreatorInput> meshCreatorInputQueue = new ConcurrentQueue<MeshCreatorInput>();
        private ConcurrentQueue<MeshSimplifierstruct> createdMeshes = new ConcurrentQueue<MeshSimplifierstruct>();
        private int createdLayers;
        private volatile bool fileRead = false;
        public float meshsimplifyquality = 1f;

        public string FolderToExport { get; private set; }
        public string modelName { get; private set; }

        private bool working = false;
        private List<string> fileNames;

        public event EventHandler<bool> MeshGenrerated;

        public void GenerateMeshFromGcode(string path, string ExportPath)
        {
            if (!working)
            {
                fileNames = new List<string>();
                modelName = Path.GetFileNameWithoutExtension(path);
                working = true;
                FolderToExport = ExportPath;
                Task.Run(() => ReadGcodeFile(path));

                meshCreatorInputQueue = new ConcurrentQueue<MeshCreatorInput>();
                createdMeshes = new ConcurrentQueue<MeshSimplifierstruct>();
                
                Task.Run(() =>
                {
                    while (!fileRead || meshCreatorInputQueue.Count > 0)
                    {
                        MeshCreatorInput mci;
                        if (meshCreatorInputQueue.TryDequeue(out mci))
                        {
                            if (mci != null)
                            {
                                CreateMesh(mci);
                            }
                        }
                    }

                    Parallel.ForEach(createdMeshes, (currentMesh) => simplify(currentMesh));
                    working = false;
                    ZipMeshes();
                    DeleteGcodeFiles();
                    MeshGenrerated?.Invoke(this, true);
                    meshCreatorInputQueue = null;
                    createdMeshes = null;
                    fileNames = null;
                });
            }
            else
            {
                MeshGenrerated?.Invoke(this, false);
            }
        }

        private void DeleteGcodeFiles()
        {
            foreach (var file in fileNames)
            {
                File.Delete(file);
            }
        }

        private void simplify(MeshSimplifierstruct currentMesh)
        {
            Mesh ToSimplify = currentMesh.ToSimplify;
            Mesh Simplified = MeshDecimator.MeshDecimation.DecimateMesh(ToSimplify, (int)(ToSimplify.VertexCount * meshsimplifyquality));

            SaveLayerAsObj(Simplified, currentMesh.name);

        }

        public void SaveLayerAsAsset(Mesh mesh, string name)
        {
            if (!Directory.Exists(FolderToExport))
            {
                Directory.CreateDirectory(FolderToExport);
            }

            //Write the mesh to disk again
            mesh.name = name;

            var filename = FolderToExport + modelName + " " + name + ".mesh";
            fileNames.Add(filename);

            File.WriteAllBytes(filename, MeshSerializer.SerializeMesh(mesh));// GOAAAL

        }

        public void SaveLayerAsObj(Mesh mesh, string name)
        {
            if (!Directory.Exists(FolderToExport))
            {
                Directory.CreateDirectory(FolderToExport);
            }

            StringBuilder sb = new StringBuilder();


            sb.Append("o " + name);
            sb.Append(Environment.NewLine);
            foreach (var vertex in mesh.Vertices)
            {
                sb.Append("v ");
                sb.Append(vertex.ToString());
                sb.Append(Environment.NewLine);
            }
            foreach (var normal in mesh.Normals)
            {
                sb.Append("vn ");
                sb.Append(normal.ToString());
                sb.Append(Environment.NewLine);
            }
            for (int i = 0; i < mesh.Indices.Length - 3; i += 3)
            {
                sb.Append("f ");
                sb.Append((mesh.Indices[i] + 1) + " " + (mesh.Indices[i + 1] + 1) + " " + (mesh.Indices[i + 2] + 1));
                sb.Append(Environment.NewLine);
            }
            var filename = FolderToExport + modelName + " " + name + ".obj";
            fileNames.Add(filename);
            System.IO.File.WriteAllText(filename, sb.ToString());

            //put everthing in one zip file
            
        }

        private void ZipMeshes()
        {
            using (ZipArchive archive = ZipFile.Open(Path.Combine(FolderToExport, modelName + ".zip"), ZipArchiveMode.Create))
            {
                foreach (var file in fileNames)
                {
                    archive.CreateEntryFromFile(file, Path.GetFileName(file));
                }
                
            }
        }

        internal void CreateMesh(MeshCreatorInput input)
        {

            Mesh mesh = new Mesh(input.newVertices, input.newTriangles);
            string meshparentname = input.meshname.Split(' ')[0];
            mesh.Vertices = input.newVertices;
            mesh.Normals = input.newNormals;
            MeshSimplifierstruct msc = new MeshSimplifierstruct();
            msc.ToSimplify = mesh;
            msc.name = input.meshname;
            createdMeshes.Enqueue(msc);
        }

        static Vector3 currentPosition;

        private bool isTraveling = false;
        string compName = string.Empty;
        int currentLayer = -1;
        float currentLayerHeight = 0f;
        float nextLayerHeight = 0f;

        public void ReadGcodeFile(string path)
        {

            Dictionary<string, List<List<Vector3>>> movesPerComponent = new Dictionary<string, List<List<Vector3>>>();
            float tmpNextLayerHeight;

            currentPosition = Vector3.zero;

            var lines = File.ReadAllLines(path);

            foreach (var line in lines)
            {
                if (LineIsMovement(line))
                {
                    if (MovesToNextLayer(line))
                    {
                        currentLayer++;
                        foreach (var keyValuePair in movesPerComponent)
                        {
                            createlayer(keyValuePair.Value, keyValuePair.Key);

                        }
                        movesPerComponent.Clear();
                        currentLayerHeight = nextLayerHeight;
                    }

                    CheckTraveling(line);
                    GetPosition(line);

                    if (IsNewPart(line) && compName != string.Empty)
                    {
                        if (!movesPerComponent.ContainsKey(compName))
                        {
                            movesPerComponent.Add(compName, new List<List<Vector3>>() { new List<Vector3>() });
                        }
                        else
                        {
                            movesPerComponent[compName].Add(new List<Vector3>());
                        }
                        if (!isTraveling)
                        {
                            movesPerComponent[compName][^1].Add(new Vector3(currentPosition.x, currentPosition.y, currentPosition.z));
                        }
                    }
                    if (AddCurrentPositionToList(line))
                    {
                        movesPerComponent[compName][^1].Add(new Vector3(currentPosition.x, currentPosition.y, currentPosition.z));
                    }
                }
                else
                {
                    if (TryGetLayerHeightFromLine(line, out tmpNextLayerHeight))
                    {
                        nextLayerHeight = tmpNextLayerHeight;
                    }

                    var tmp = GetComponentName(line);
                    if (tmp != compName)
                    {
                        compName = tmp.ToLower();
                        if (!movesPerComponent.ContainsKey(compName))
                        {
                            movesPerComponent.Add(compName, new List<List<Vector3>>() { new List<Vector3>() });
                            movesPerComponent[compName][^1].Add(new Vector3(currentPosition.x, currentPosition.y, currentPosition.z));
                        }
                        else
                        {
                            movesPerComponent[compName].Add(new List<Vector3>());
                            movesPerComponent[compName][^1].Add(new Vector3(currentPosition.x, currentPosition.y, currentPosition.z));
                        }
                    }
                }
            }
        }

        private bool LineIsMovement(string line)
        {
            return line.StartsWith("G1");
        }
        private bool MovesToNextLayer(string line)
        {
            return line.Contains("move to next layer");
        }

        private void CheckTraveling(string line)
        {
            if (line.Contains("; lift Z") && !isTraveling)
            {
                isTraveling = true;
                return;
            }

            if (line.Contains("; restore layer Z") && isTraveling)
            {
                isTraveling = false;
                return;
            }

        }

        private void GetPosition(string line)
        {
            var parts = line.Split(' ');
            foreach (var part in parts)
            {
                if (part.StartsWith('X'))
                {
                    currentPosition.x = float.Parse(part.Substring(1, part.Length - 1), CultureInfo.InvariantCulture.NumberFormat);
                }
                else if (part.StartsWith('Y'))
                {
                    currentPosition.z = float.Parse(part.Substring(1, part.Length - 1), CultureInfo.InvariantCulture.NumberFormat);
                }
                else if (part.StartsWith('Z') && part.Length > 1)
                {
                    currentPosition.y = float.Parse(part.Substring(1, part.Length - 1), CultureInfo.InvariantCulture.NumberFormat);
                }
            }
        }

        private bool AddCurrentPositionToList(string line)
        {
            return !isTraveling && (line.Contains("; skirt") || line.Contains("; perimeter") || line.Contains("; infill") || line.Contains("; support"));
        }

        private bool IsNewPart(string line)
        {
            return line.Contains("move to first");
        }

        private bool TryGetLayerHeightFromLine(string line, out float layerHeight)
        {
            if (line.Contains(";HEIGHT"))
            {
                var tmp = line.Split(':');
                layerHeight = float.Parse(tmp[1], CultureInfo.InvariantCulture.NumberFormat);
                return true;
            }
            layerHeight = 0;
            return false;
        }

        private string GetComponentName(string line)
        {
            if (line.StartsWith(";TYPE:") && line != ";TYPE:Custom")
            {
                return line.Substring(6);
            }
            return compName;
        }

        void createlayer(List<List<Vector3>> tmpmoves, string meshname)
        {
            List<Vector3d> newVertices = new List<Vector3d>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector2> newUV = new List<Vector2>();
            List<int> newTriangles = new List<int>();
            List<Dictionary<int, Dictionary<int, int>>> neighbours = new List<Dictionary<int, Dictionary<int, int>>>();
            for (int tmpmvn = 0; tmpmvn < tmpmoves.Count; tmpmvn++)
            {
                List<Vector3> tmpmove = tmpmoves[tmpmvn];

                if (tmpmove.Count > 1)
                {
                    createMesh(ref tmpmove, ref newVertices,  ref newNormals, ref newUV, ref newTriangles);
                }
            }
            MeshCreatorInput mci = new MeshCreatorInput
            {
                meshname = meshname,
                newUV = newUV.ToArray(),
                newNormals = newNormals.ToArray(),
                newVertices = newVertices.ToArray(),
                newTriangles = newTriangles.ToArray()
            };
            meshCreatorInputQueue.Enqueue(mci);
            createdLayers++;
        }


        internal void createMesh(ref List<Vector3> tmpmove, ref List<Vector3d> newVertices,  ref List<Vector3> newNormals, ref List<Vector2> newUV, ref List<int> newTriangles)
        {

            //here i generate the mesh from the tmpmove list, wich is a list of points the extruder goes to
            int vstart = newVertices.Count;
            Vector3 dv = tmpmove[1] - tmpmove[0];
            Vector3 dvt = dv; dvt.x = dv.z; dvt.z = -dv.x;
            dvt = -dvt.Normalized;
            newVertices.Add(tmpmove[0] - dv.Normalized * 0.5f + dvt * plasticwidth * 0.5f);
            newVertices.Add(tmpmove[0] - dv.Normalized * 0.5f - dvt * 0.5f * plasticwidth);
            newVertices.Add(tmpmove[0] - dv.Normalized * 0.5f - dvt * 0.5f * plasticwidth - new Vector3(0, -0.25f, 0) * layercluster);
            newVertices.Add(tmpmove[0] - dv.Normalized * 0.5f + dvt * plasticwidth * 0.5f - new Vector3(0, -0.25f, 0) * layercluster);
            newNormals.Add((dvt.Normalized * plasticwidth / 2 + new Vector3(0, plasticwidth / 2, 0) - dv.Normalized * plasticwidth / 2).Normalized);
            newNormals.Add((dvt.Normalized * -plasticwidth / 2 + new Vector3(0, plasticwidth / 2, 0) - dv.Normalized * plasticwidth / 2).Normalized);
            newNormals.Add((dvt.Normalized * -plasticwidth / 2 + new Vector3(0, -plasticwidth / 2, 0) - dv.Normalized * plasticwidth / 2).Normalized);
            newNormals.Add((dvt.Normalized * plasticwidth / 2 + new Vector3(0, -plasticwidth / 2, 0) - dv.Normalized * plasticwidth / 2).Normalized);
            newUV.Add(new Vector2(0.0f, 0.0f));
            newUV.Add(new Vector2(0.0f, 1.0f));
            newUV.Add(new Vector2(1.0f, 1.0f));
            newUV.Add(new Vector2(1.0f, 0.0f));

            newTriangles.Add(vstart + 2);
            newTriangles.Add(vstart + 1);
            newTriangles.Add(vstart + 0); //back (those need to be in clockwise orientation for culling to work right)
            newTriangles.Add(vstart + 0);
            newTriangles.Add(vstart + 3);
            newTriangles.Add(vstart + 2);


            for (int i = 1; i < tmpmove.Count - 1; i++)
            {

                Vector3 dv1 = tmpmove[i] - tmpmove[i - 1];
                Vector3 dvt1 = dv1; dvt1.x = dv1.z; dvt1.z = -dv1.x;
                Vector3 dv2 = tmpmove[i + 1] - tmpmove[i];
                Vector3 dvt2 = dv2; dvt2.x = dv2.z; dvt2.z = -dv2.x;
                dvt = (dvt1 + dvt2).Normalized * -plasticwidth;
                newVertices.Add(tmpmove[i] + dvt * 0.5f);
                newVertices.Add(tmpmove[i] - dvt * 0.5f);
                newVertices.Add(tmpmove[i] - dvt * 0.5f - new Vector3(0, -0.25f, 0) * layercluster);
                newVertices.Add(tmpmove[i] + dvt * 0.5f - new Vector3(0, -0.25f, 0) * layercluster);
                newNormals.Add((dvt.Normalized + new Vector3(0, 0.125f, 0)).Normalized);
                newNormals.Add((dvt.Normalized + new Vector3(0, 0.125f, 0)).Normalized);
                newNormals.Add((dvt.Normalized + new Vector3(0, -0.125f, 0)).Normalized);
                newNormals.Add((dvt.Normalized + new Vector3(0, -0.125f, 0)).Normalized);
                newUV.Add(new Vector2(0.0f, 0.0f));
                newUV.Add(new Vector2(0.0f, 1.0f));
                newUV.Add(new Vector2(1.0f, 1.0f));
                newUV.Add(new Vector2(1.0f, 0.0f));

                newTriangles.Add(vstart + 0 + 4 * (i - 1));
                newTriangles.Add(vstart + 1 + 4 * (i - 1));
                newTriangles.Add(vstart + 5 + 4 * (i - 1)); //top
                newTriangles.Add(vstart + 0 + 4 * (i - 1));
                newTriangles.Add(vstart + 5 + 4 * (i - 1));
                newTriangles.Add(vstart + 4 + 4 * (i - 1));

                newTriangles.Add(vstart + 1 + 4 * (i - 1));
                newTriangles.Add(vstart + 2 + 4 * (i - 1));
                newTriangles.Add(vstart + 6 + 4 * (i - 1));//left
                newTriangles.Add(vstart + 1 + 4 * (i - 1));
                newTriangles.Add(vstart + 6 + 4 * (i - 1));
                newTriangles.Add(vstart + 5 + 4 * (i - 1));

                newTriangles.Add(vstart + 0 + 4 * (i - 1));
                newTriangles.Add(vstart + 4 + 4 * (i - 1));
                newTriangles.Add(vstart + 3 + 4 * (i - 1));//right
                newTriangles.Add(vstart + 3 + 4 * (i - 1));
                newTriangles.Add(vstart + 4 + 4 * (i - 1));
                newTriangles.Add(vstart + 7 + 4 * (i - 1));

                newTriangles.Add(vstart + 2 + 4 * (i - 1));
                newTriangles.Add(vstart + 3 + 4 * (i - 1));
                newTriangles.Add(vstart + 7 + 4 * (i - 1));//bottom
                newTriangles.Add(vstart + 2 + 4 * (i - 1));
                newTriangles.Add(vstart + 7 + 4 * (i - 1));
                newTriangles.Add(vstart + 6 + 4 * (i - 1));
            }

            dv = tmpmove[tmpmove.Count - 1] - tmpmove[tmpmove.Count - 2];
            dvt = dv; dvt.x = dv.z; dvt.z = -dv.x;
            dvt = dvt.Normalized * plasticwidth;
            dv = dv.Normalized * plasticwidth / 2;
            int maxi = tmpmove.Count - 2;

            newVertices.Add(tmpmove[maxi] + dv + dvt * 0.5f);
            newVertices.Add(tmpmove[maxi] + dv - dvt * 0.5f);
            newVertices.Add(tmpmove[maxi] + dv - dvt * 0.5f - new Vector3(0, -0.25f, 0) * layercluster);
            newVertices.Add(tmpmove[maxi] + dv + dvt * 0.5f - new Vector3(0, -0.25f, 0) * layercluster);
            newNormals.Add((dvt + new Vector3(0, plasticwidth / 2, 0) + dv).Normalized);
            newNormals.Add((-dvt + new Vector3(0, plasticwidth / 2, 0) + dv).Normalized);
            newNormals.Add((-dvt + new Vector3(0, -plasticwidth / 2, 0) + dv).Normalized);
            newNormals.Add((dvt + new Vector3(0, -plasticwidth / 2, 0) + dv).Normalized);
            newUV.Add(new Vector2(0.0f, 0.0f));
            newUV.Add(new Vector2(0.0f, 1.0f));
            newUV.Add(new Vector2(1.0f, 1.0f));
            newUV.Add(new Vector2(1.0f, 0.0f));

            newTriangles.Add(vstart + 0 + 4 * maxi);
            newTriangles.Add(vstart + 1 + 4 * maxi);
            newTriangles.Add(vstart + 5 + 4 * maxi); //top
            newTriangles.Add(vstart + 0 + 4 * maxi);
            newTriangles.Add(vstart + 5 + 4 * maxi);
            newTriangles.Add(vstart + 4 + 4 * maxi);

            newTriangles.Add(vstart + 1 + 4 * maxi);
            newTriangles.Add(vstart + 2 + 4 * maxi);
            newTriangles.Add(vstart + 6 + 4 * maxi);//left
            newTriangles.Add(vstart + 1 + 4 * maxi);
            newTriangles.Add(vstart + 6 + 4 * maxi);
            newTriangles.Add(vstart + 5 + 4 * maxi);

            newTriangles.Add(vstart + 0 + 4 * maxi);
            newTriangles.Add(vstart + 4 + 4 * maxi);
            newTriangles.Add(vstart + 3 + 4 * maxi);//right
            newTriangles.Add(vstart + 3 + 4 * maxi);
            newTriangles.Add(vstart + 4 + 4 * maxi);
            newTriangles.Add(vstart + 7 + 4 * maxi);

            newTriangles.Add(vstart + 2 + 4 * maxi);
            newTriangles.Add(vstart + 3 + 4 * maxi);
            newTriangles.Add(vstart + 7 + 4 * maxi);//bottom
            newTriangles.Add(vstart + 2 + 4 * maxi);
            newTriangles.Add(vstart + 7 + 4 * maxi);
            newTriangles.Add(vstart + 6 + 4 * maxi);

            newTriangles.Add(vstart + 4 + 4 * maxi);
            newTriangles.Add(vstart + 5 + 4 * maxi);
            newTriangles.Add(vstart + 7 + 4 * maxi);//front
            newTriangles.Add(vstart + 7 + 4 * maxi);
            newTriangles.Add(vstart + 5 + 4 * maxi);
            newTriangles.Add(vstart + 6 + 4 * maxi);

        }
    }
}
