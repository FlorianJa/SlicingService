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

        private float plasticwidth = 0.45f;
        private ConcurrentQueue<List<MeshCreatorInput>> meshCreatorInputQueue = new ConcurrentQueue<List<MeshCreatorInput>>();
        private ConcurrentQueue<List<Mesh>> createdMeshes = new ConcurrentQueue<List<Mesh>>();
        private ConcurrentDictionary<string, ConcurrentBag<Mesh>> toMergeMeshes = new ConcurrentDictionary<string, ConcurrentBag<Mesh>>();

        private int createdLayers;
        private volatile bool fileRead = false;
        public float meshsimplifyquality = 0.75f;

        static Vector3 currentPosition;

        private bool isTraveling = false;
        string compName = string.Empty;
        int currentLayer = -1;
        float currentLayerHeight = 0f;
        float nextLayerHeight = 0f;

        public string FolderToExport { get; private set; }
        public string modelName { get; private set; }

        private bool working = false;
        private ConcurrentBag<string> fileNames;
        private bool mergeLayers = true;
        private bool ExportAsOBJ = true;

        public event EventHandler<bool> MeshGenrerated;

        public void GenerateMeshFromGcode(string path, string ExportPath)
        {
            if (!working)
            {
                fileNames = new ConcurrentBag<string>();
                modelName = Path.GetFileNameWithoutExtension(path);
                working = true;
                FolderToExport = ExportPath;
                Task.Run(() => ReadGcodeFile(path));

                meshCreatorInputQueue = new ConcurrentQueue<List<MeshCreatorInput>>();
                createdMeshes = new ConcurrentQueue<List<Mesh>>();

                int numProcs = Environment.ProcessorCount;
                int concurrencyLevel = numProcs * 2;

                toMergeMeshes = new ConcurrentDictionary<string, ConcurrentBag<Mesh>>(concurrencyLevel, 10); //there shouldnt be more than 10 different components

                Task.Run(() =>
                {
                    while (!fileRead || meshCreatorInputQueue.Count > 0)
                    {
                        List<MeshCreatorInput> mci;
                        if (meshCreatorInputQueue.TryDequeue(out mci))
                        {
                            if (mci != null)
                            {
                                CreateMesh(mci);
                            }
                        }
                    }

                    Parallel.ForEach(createdMeshes, (currentMesh) => simplify(currentMesh));
                    
                    if(mergeLayers)
                    {
                        Parallel.ForEach(toMergeMeshes, (component) => { 
                                               
                            var tmp = component.Value.ToList().OrderBy(x => x.layer);
                            
                            int partCounter = 0;
                            var mergedMeshes = new List<Mesh>();
                            int triangleCount = 0;
                            foreach (var mesh in tmp)
                            {
                                
                                if(triangleCount+ mesh.TriangleCount < 65000)// Unity max triangle count is around 650000
                                {
                                    triangleCount += mesh.TriangleCount;
                                    mergedMeshes.Add(mesh);
                                }
                                else
                                {
                                    mergedMeshes[0].name += "-" + partCounter;
                                    if (ExportAsOBJ)
                                    {
                                        SaveLayerAsObj(mergedMeshes);
                                    }
                                    else
                                    {
                                        SaveLayerAsAsset(mergedMeshes);
                                    }
                                    mergedMeshes.Clear();
                                    mergedMeshes.Add(mesh);
                                    partCounter++;
                                    triangleCount = mesh.TriangleCount;
                                }
                            }

                            if (mergedMeshes.Count > 0)
                            {
                                mergedMeshes[0].name += "-" + partCounter;
                                if (ExportAsOBJ)
                                {
                                    SaveLayerAsObj(mergedMeshes);
                                }
                                else
                                {
                                    SaveLayerAsAsset(mergedMeshes);
                                }
                                mergedMeshes.Clear();
                            }
                        
                        });

                    }

                    ZipMeshes();
                    DeleteGcodeFiles();
                    MeshGenrerated?.Invoke(this, true);

                    meshCreatorInputQueue.Clear();
                    meshCreatorInputQueue = null;
                    createdMeshes.Clear();
                    createdMeshes = null;
                    toMergeMeshes.Clear();
                    toMergeMeshes = null;
                    fileNames = null;
                    working = false;
                }).ContinueWith((task) => {
                    GC.Collect();
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

        private void simplify(List<Mesh> inputMeshes)
        {

            List<Mesh> meshes = new List<Mesh>();

            foreach (var mesh in inputMeshes)
            {
                var tmp = GcodeToMesh.MeshDecimator.MeshDecimation.DecimateMesh(mesh, (int)(mesh.VertexCount * meshsimplifyquality));
                tmp.name = mesh.name;
                tmp.layer = mesh.layer;
                meshes.Add(tmp);

            }

            if (mergeLayers)
            {
                foreach (var mesh in meshes)
                {
                    if (!toMergeMeshes.ContainsKey(mesh.name))
                    {
                        toMergeMeshes.TryAdd(mesh.name, new ConcurrentBag<Mesh>());
                    }

                    toMergeMeshes[mesh.name].Add(mesh);
                }
            }
            else
            {
                if (ExportAsOBJ)
                {
                    SaveLayerAsObj(meshes);
                }
                else
                {
                    SaveLayerAsAsset(meshes);
                }
            }
        }
        private void SaveLayerAsAsset(List<Mesh> meshes)
        {
            if (!Directory.Exists(FolderToExport))
            {
                Directory.CreateDirectory(FolderToExport);
            }

            string fileName;
            var tmpVertices = new List<Vector3d>();
            var tmpIndices = new List<int>();
            foreach (var mesh in meshes)
            {
                if (mergeLayers)
                {
                    tmpVertices.AddRange(mesh.Vertices);
                    tmpIndices.AddRange(mesh.Indices); 
                }
                else
                {
                    fileName = Path.Combine(FolderToExport, modelName + "-" + meshes[0].name + "-" + meshes[0].layer + ".mesh");
                    fileNames.Add(fileName);
                    //Write the mesh to disk again
                    File.WriteAllBytes(Path.Combine(FolderToExport, fileName), MeshSerializer.SerializeMesh(mesh));
                }
            }

            if (mergeLayers)
            {
                fileName = Path.Combine(FolderToExport, modelName + "-" + meshes[0].name + ".mesh");
                fileNames.Add(fileName);
                var exportMesh = new Mesh(tmpVertices.ToArray(),tmpIndices.ToArray());
                exportMesh.RecalculateNormals();
                File.WriteAllBytes(Path.Combine(FolderToExport, fileName), MeshSerializer.SerializeMesh(exportMesh));
            }

        }
            public void SaveLayerAsObj(List<Mesh> meshes)
        {
            if (!Directory.Exists(FolderToExport))
            {
                Directory.CreateDirectory(FolderToExport);
            }
            int offset = 0;
            StringBuilder sb = new StringBuilder();

            if (meshes.Count > 0)
            {
                foreach (var mesh in meshes)
                {
                    //sb.Append(Environment.NewLine);
                    //sb.Append("o " + counter++);
                    //sb.Append(Environment.NewLine);
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
                    sb.Append("s off");
                    sb.Append(Environment.NewLine);
                    for (int i = 0; i <= mesh.Indices.Length - 3; i += 3)
                    {
                        sb.Append("f ");
                        sb.Append((mesh.Indices[i] + 1 + offset) + "//" + (mesh.Indices[i] + 1 + offset) + " " +
                                (mesh.Indices[i + 1] + 1 + offset) + "//" + (mesh.Indices[i + 1] + 1 + offset) + " " +
                                (mesh.Indices[i + 2] + 1 + offset) + "//" + (mesh.Indices[i + 2] + 1 + offset) + " ");
                        sb.Append(Environment.NewLine);
                    }
                    offset += mesh.Indices.Max() + 1;
                }

                string fileName = string.Empty;
                if (!mergeLayers)
                {
                    fileName = Path.Combine(FolderToExport, modelName + "-" + meshes[0].name + "-" + meshes[0].layer + ".obj");
                }
                else
                {
                    fileName = Path.Combine(FolderToExport, modelName + "-" + meshes[0].name + ".obj");
                }

                fileNames.Add(fileName);
                System.IO.File.WriteAllText(fileName, sb.ToString());
            }
        }

        private void ZipMeshes()
        {
            if(File.Exists(Path.Combine(FolderToExport, modelName + ".zip")))
            {
                File.Delete(Path.Combine(FolderToExport, modelName + ".zip"));
            }

            using (ZipArchive archive = ZipFile.Open(Path.Combine(FolderToExport, modelName + ".zip"), ZipArchiveMode.Create))
            {
                foreach (var file in fileNames)
                {
                    archive.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Fastest);
                }
                
            }
        }

        internal void CreateMesh(List<MeshCreatorInput> input)
        {
            List<Mesh> meshes = new List<Mesh>();
            foreach (var part in input)
            {
                Mesh mesh = new Mesh(part.newVertices, part.newTriangles);
                mesh.name = part.meshname;
                mesh.layer = part.layer;
                mesh.Vertices = part.newVertices;
                mesh.RecalculateNormals();

                if (mesh.VertexCount > 0)
                {
                    meshes.Add(mesh);
                }
            }
            createdMeshes.Enqueue(meshes);


        }

        public void ReadGcodeFile(string path)
        {
            fileRead = false;
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
                            CreateComponentsInLayer(keyValuePair.Value, keyValuePair.Key);

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

            fileRead = true;
            movesPerComponent.Clear();
            movesPerComponent = null;
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

        void CreateComponentsInLayer(List<List<Vector3>> tmpmoves, string meshname)
        {
            List<MeshCreatorInput> tmp = new List<MeshCreatorInput>();

            foreach (var moves in tmpmoves)
            {
                if (moves.Count > 1)
                {
                    var rawMesh = CreateRawMesh(moves, meshname,  currentLayer);
                    if (rawMesh != null)
                    {
                        tmp.Add(rawMesh);
                    }
                }
            }
            meshCreatorInputQueue.Enqueue(tmp);
        }

        internal MeshCreatorInput CreateRawMesh(List<Vector3> tmpmove, string name, int layer)
        {
            if (tmpmove.Count <= 1)
            {
                return null;
            }

            List<Vector3d> newVertices = new List<Vector3d>();
            List<Vector3> newNormals = new List<Vector3>();
            List<Vector2> newUV = new List<Vector2>();
            List<int> newTriangles = new List<int>();

            //float plasticwidth = 0.5f;

            Vector3 dv = tmpmove[1] - tmpmove[0];
            Vector3 dvt = dv; dvt.x = dv.z; dvt.z = -dv.x;
            dvt = -dvt.Normalized;
            Vector3 LayerHeightVector = new Vector3(0, currentLayerHeight, 0);

            Vector3 HalfLayerHeightVector = new Vector3(0, currentLayerHeight / 2f, 0);

            newVertices.Add(tmpmove[0] - HalfLayerHeightVector);
            newVertices.Add(tmpmove[0] - dvt * plasticwidth / 2f);
            newVertices.Add(tmpmove[0] + HalfLayerHeightVector);
            newVertices.Add(tmpmove[0] + dvt * plasticwidth / 2f);
            
            newUV.Add(new Vector2(0.0f, 0.0f));
            newUV.Add(new Vector2(0.0f, 1.0f));
            newUV.Add(new Vector2(1.0f, 1.0f));
            newUV.Add(new Vector2(1.0f, 0.0f));
            
            newTriangles.Add(2);
            newTriangles.Add(1);
            newTriangles.Add(0); //back (those need to be in clockwise orientation for culling to work right)
            newTriangles.Add(0);
            newTriangles.Add(3);
            newTriangles.Add(2);


            for (int i = 1; i < tmpmove.Count - 1; i++)
            {

                Vector3 dv1 = tmpmove[i] - tmpmove[i - 1];
                Vector3 dvt1 = dv1; dvt1.x = dv1.z; dvt1.z = -dv1.x;
                Vector3 dv2 = tmpmove[i + 1] - tmpmove[i];
                Vector3 dvt2 = dv2; dvt2.x = dv2.z; dvt2.z = -dv2.x;
                dvt = (dvt1 + dvt2).Normalized * -plasticwidth / 2;

                newVertices.Add(tmpmove[i] - HalfLayerHeightVector);
                newVertices.Add(tmpmove[i] - dvt);
                newVertices.Add(tmpmove[i] + HalfLayerHeightVector);
                newVertices.Add(tmpmove[i] + dvt);
                
                newUV.Add(new Vector2(0.0f, 0.0f));
                newUV.Add(new Vector2(0.0f, 1.0f));
                newUV.Add(new Vector2(1.0f, 1.0f));
                newUV.Add(new Vector2(1.0f, 0.0f));

                newTriangles.Add(0 + 4 * (i - 1));
                newTriangles.Add(1 + 4 * (i - 1));
                newTriangles.Add(5 + 4 * (i - 1)); //top
                newTriangles.Add(0 + 4 * (i - 1));
                newTriangles.Add(5 + 4 * (i - 1));
                newTriangles.Add(4 + 4 * (i - 1));

                newTriangles.Add(1 + 4 * (i - 1));
                newTriangles.Add(2 + 4 * (i - 1));
                newTriangles.Add(6 + 4 * (i - 1));//left
                newTriangles.Add(1 + 4 * (i - 1));
                newTriangles.Add(6 + 4 * (i - 1));
                newTriangles.Add(5 + 4 * (i - 1));

                newTriangles.Add(0 + 4 * (i - 1));
                newTriangles.Add(4 + 4 * (i - 1));
                newTriangles.Add(3 + 4 * (i - 1));//right
                newTriangles.Add(3 + 4 * (i - 1));
                newTriangles.Add(4 + 4 * (i - 1));
                newTriangles.Add(7 + 4 * (i - 1));

                newTriangles.Add(2 + 4 * (i - 1));
                newTriangles.Add(3 + 4 * (i - 1));
                newTriangles.Add(7 + 4 * (i - 1));//bottom
                newTriangles.Add(2 + 4 * (i - 1));
                newTriangles.Add(7 + 4 * (i - 1));
                newTriangles.Add(6 + 4 * (i - 1));
            }

            dv = tmpmove[tmpmove.Count - 1] - tmpmove[tmpmove.Count - 2];
            dvt = dv; dvt.x = dv.z; dvt.z = -dv.x;
            dvt = -dvt.Normalized * plasticwidth / 2;
            dv = dv.Normalized * plasticwidth / 2;
            int maxi = tmpmove.Count - 2;

            newVertices.Add(tmpmove[^1] - HalfLayerHeightVector);
            newVertices.Add(tmpmove[^1] - dvt);
            newVertices.Add(tmpmove[^1] + HalfLayerHeightVector);
            newVertices.Add(tmpmove[^1] + dvt);
            
            newUV.Add(new Vector2(0.0f, 0.0f));
            newUV.Add(new Vector2(0.0f, 1.0f));
            newUV.Add(new Vector2(1.0f, 1.0f));
            newUV.Add(new Vector2(1.0f, 0.0f));

            newTriangles.Add(0 + 4 * maxi);
            newTriangles.Add(1 + 4 * maxi);
            newTriangles.Add(5 + 4 * maxi); //top
            newTriangles.Add(0 + 4 * maxi);
            newTriangles.Add(5 + 4 * maxi);
            newTriangles.Add(4 + 4 * maxi);

            newTriangles.Add(1 + 4 * maxi);
            newTriangles.Add(2 + 4 * maxi);
            newTriangles.Add(6 + 4 * maxi);//left
            newTriangles.Add(1 + 4 * maxi);
            newTriangles.Add(6 + 4 * maxi);
            newTriangles.Add(5 + 4 * maxi);

            newTriangles.Add(0 + 4 * maxi);
            newTriangles.Add(4 + 4 * maxi);
            newTriangles.Add(3 + 4 * maxi);//right
            newTriangles.Add(3 + 4 * maxi);
            newTriangles.Add(4 + 4 * maxi);
            newTriangles.Add(7 + 4 * maxi);

            newTriangles.Add(2 + 4 * maxi);
            newTriangles.Add(3 + 4 * maxi);
            newTriangles.Add(7 + 4 * maxi);//bottom
            newTriangles.Add(2 + 4 * maxi);
            newTriangles.Add(7 + 4 * maxi);
            newTriangles.Add(6 + 4 * maxi);

            newTriangles.Add(4 + 4 * maxi);
            newTriangles.Add(5 + 4 * maxi);
            newTriangles.Add(7 + 4 * maxi);//front
            newTriangles.Add(7 + 4 * maxi);
            newTriangles.Add(5 + 4 * maxi);
            newTriangles.Add(6 + 4 * maxi);


            return new MeshCreatorInput
            {
                meshname = name,
                layer = layer,
                newUV = newUV.ToArray(),
                newNormals = newNormals.ToArray(),
                newVertices = newVertices.ToArray(),
                newTriangles = newTriangles.ToArray()
            };
        }
    }
}
