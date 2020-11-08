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
            File.WriteAllBytes(FolderToExport + name + ".mesh", MeshSerializer.SerializeMesh(mesh));// GOAAAL

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

        public void ReadGcodeFile(string path)
        {

            StreamReader reader = new StreamReader(new FileStream(path, FileMode.Open));
            
            //mc.print("loading " + filename);
            List<string> meshnames = new List<string>();
            int currentmesh = -1;
            Dictionary<string, List<List<Vector3>>> tmpmove = new Dictionary<string, List<List<Vector3>>>();
            Vector3 currpos = new Vector3(0, 0, 0);
            float accumulateddist = 0.0f;
            Vector3 lastpointcache = new Vector3(0, 0, 0);
            int linesread = 0;
            int layernum = -1;
            bool accumulating = false;
            float lastanglecache = 0.0f;
            float accumulatedangle = 0.0f;
            bool ismesh = false;
            //bool islayerheight = false;
            string line = reader.ReadLine();
            while(line != null)
            {
                //Layerheigt is defined in Prusa by writing ";AFTER_LAYER_CHANGE" and in the next line writing the height, therefore this happens:
                linesread += 1;
                if (line.Contains("support"))
                {
                    bool ishere = true;
                }
                bool isnotmeshmove = IsLineMesh(line);
                if (line.Contains("move to next layer"))
                {
                    layernum = layernum + 1;
                    currpos.y = GetYPosition(line);
                    //islayerheight = true;
                    foreach (string namepart in tmpmove.Keys)
                    {
                        createlayer(tmpmove[namepart], namepart);
                    }
                    tmpmove.Clear();
                }
                //movement commands are all G0 or G1 in Prusa Gcode

                else if ((line.StartsWith("G1") || line.StartsWith("G0")) && layernum != -1 && ((layernum % layercluster) == 0 || layercluster == 1))
                {
                    //bool isnew = false;
                    if (line.Contains(";") && !isnotmeshmove)
                    {
                        string namemesh = ExtractMeshName(layernum, line);//In Prusaslicer the comments about what the Line Means are right next to the line

                        if (!meshnames.Contains(namemesh))
                        {
                            //isnew = true;
                            meshnames.Add(namemesh);
                            currentmesh = meshnames.Count - 1;
                            tmpmove[namemesh] = new List<List<Vector3>>();
                            tmpmove[namemesh].Add(new List<Vector3>());
                        }
                        else
                        {
                            if (meshnames[currentmesh] != namemesh || !ismesh)//Sometimes a type like infill happens more often inside one layer
                            {
                                tmpmove[namemesh].Add(new List<Vector3>());
                            }
                        }
                        ismesh = true;
                    }

                    string[] parts = line.Split(';')[0].Split(' ');
                    if (line.Contains(";") && !isnotmeshmove)
                    {
                                                //Since The G1 or G0 Commands are just "go to" commands, we need to store the Previous position as well, so before we touch currpos, we add it to the mesh, but only once per mesh
                        if (!accumulating &&
                            (line.Contains("X") || line.Contains("Y") || line.Contains("Z")) &&
                            line.Contains("E") &&
                            currpos.x != 0 && currpos.z != 0
                            && currentmesh != -1)
                        {
                            string meshname = meshnames[currentmesh];
                            if (tmpmove.ContainsKey(meshname))
                            {
                                tmpmove[meshname][tmpmove[meshname].Count - 1].Add(currpos);
                            }

                        }

                        //now we can update currpos
                        foreach (string part in parts)
                        {
                            if (part.Length > 0 && part[0] == 'X')
                            {
                                currpos.x = float.Parse(part.Substring(1), CultureInfo.InvariantCulture.NumberFormat);
                            }
                            else if (part.Length > 0 && part[0] == 'Y')
                            {
                                currpos.z = float.Parse(part.Substring(1), CultureInfo.InvariantCulture.NumberFormat); //Unity has a Lefthanded Coordinate System (Y up), Gcode a Righthanded (Z up)
                            }
                            else if (part.Length > 0 && part[0] == 'Z')
                            {
                                currpos.y = float.Parse(part.Substring(1), CultureInfo.InvariantCulture.NumberFormat);
                            }
                        }
                        if (((!accumulating /*|| accumulateddist > gcodeHandler.distanceclustersize || accumulatedangle > gcodeHandler.rotationclustersize*/) && (ismesh || line.Contains("E"))) && (line.Contains("X") || line.Contains("Y") || line.Contains("Z")) && currpos != new Vector3(0, 0, 0))
                        {
                            if (currentmesh != -1 )
                            {
                                string meshname = meshnames[currentmesh];
                                tmpmove[meshname][tmpmove[meshname].Count - 1].Add(currpos);
                            }
                        }
                    }
                    else
                    {
                        foreach (string part in parts)
                        {
                            if (part.Length > 0 && part[0] == 'X')
                            {
                                currpos.x = float.Parse(part.Substring(1), CultureInfo.InvariantCulture.NumberFormat);
                            }
                            else if (part.Length > 0 && part[0] == 'Y')
                            {
                                currpos.z = float.Parse(part.Substring(1), CultureInfo.InvariantCulture.NumberFormat); //Unity has a Lefthanded Coordinate System (Y up), Gcode a Righthanded (Z up)
                            }
                            else if (part.Length > 0 && part[0] == 'Z')
                            {
                                if (part.Length > 1)
                                    currpos.y = float.Parse(part.Substring(1), CultureInfo.InvariantCulture.NumberFormat);
                            }
                        }
                    }
                }
                if (line.StartsWith(";BEFORE-LAYER-CHANGE") || line.Contains("retract"))
                {
                    ismesh = false;
                }
                line = reader.ReadLine();
            }

            fileRead = true;
            tmpmove.Clear();
            tmpmove = null;
            meshnames.Clear();
            meshnames = null;
        }

        private static string ExtractMeshName(int layernum, string line)
        {
            return line.Split(';')[1].Split(' ')[1] + " " + layernum.ToString(CultureInfo.InvariantCulture);
        }

        private static float GetYPosition(string line)
        {
            return float.Parse(line.Split('Z')[1].Split(' ')[0], CultureInfo.InvariantCulture.NumberFormat);
        }

        private static bool IsLineMesh(string line)
        {
            return line.Contains("wipe and retract") || 
                    line.Contains("move to first") || 
                    line.Contains("move inwards before travel") || 
                    line.Contains("retract") ||
                    line.Contains("lift Z") || 
                    line.Contains("move to first perimeter point") || 
                    line.Contains("restore layer Z") || 
                    line.Contains("unretract") ||
                    line.Contains("Move") || 
                    line.Contains("home");
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
