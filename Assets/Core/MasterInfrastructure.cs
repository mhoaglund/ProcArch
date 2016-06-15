using System;
using UnityEngine;
using System.Collections;
using ProceduralToolkit;
using System.Collections.Generic;
using Assets.Core;
using System.Linq;
using UnityEngine.UI;

namespace Assets.Core
{
    class MasterInfrastructure : MonoBehaviour
    {
        private const int StructuresLB = 20;
        private const int StructuresUB = 44;

        private int maxStructuresX;
        private int maxStructuresZ;

        private float slackLB = 0.05f;
        private float slackUB = 0.31f;
        private float slack;

        private float cableSizeLB = 0.03f;
        private float cableSizeUB = 0.10f;
        private float cableSize;

        private float baseGrottoDivisor = 2.0f;

        //Horizontal orientation
        public enum Orientation
        {
            X,
            Z
        }

        public Material[] _materials = new Material[1];
        public Material[] _transMaterials = new Material[1];
        public Material[] _markerMaterials = new Material[1];
        public Material[] _cavewalls = new Material[1];
        private int Structures;
        public List<LibraryGenerator> bldgs = new List<LibraryGenerator>();
        public List<List<LibraryGenerator>> librows = new List<List<LibraryGenerator>>();
        public List<List<LibraryGenerator>> libcols = new List<List<LibraryGenerator>>();
        private Image MainImg;

        public Vector3 citycenter = new Vector3();

        void Start() {
            _cavewalls[0] = Resources.Load("CaveWall") as Material;
            _materials[0] = Resources.Load("StandardMod") as Material;
            _transMaterials[0] = Resources.Load("Glass") as Material;
            _markerMaterials[0] = Resources.Load("CaveWall") as Material;
            //Cursor.visible = false;
            InitGUI();
            Generate();
            
        }

        void InitGUI()
        {
            GameObject go = Instantiate(Resources.Load("UIHost")) as GameObject;
            go.AddComponent<Image>();
            go.name = "Prompt";
            var MainCanvas = go.GetComponent<Canvas>();
            MainCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            MainCanvas.worldCamera = Camera.main;
            MainCanvas.planeDistance = Camera.main.nearClipPlane + 0.01f;
            MainImg = go.GetComponent<Image>();
            MainImg.color = Color.black;
            
            //var MainText = go.GetComponent<Text>();
            //MainText.font = Resources.GetBuiltinResource(typeof(Font), "Arial.ttf") as Font;
            //MainText.fontSize = 36;
            //MainText.alignment = TextAnchor.UpperCenter;
            //MainText.text = "Hello World";
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                Generate();
            }
            // Fade the texture to clear.
            FadeOverlay();

            // If the texture is almost clear...
            if (MainImg.color.a <= 0.05f)
            {
                // ... set the colour to clear and disable the RawImage.
                MainImg.color = Color.clear;
                MainImg.enabled = false;
            }
        }

        void FadeOverlay()
        {
            MainImg.color = Color.Lerp(MainImg.color, Color.clear, 1.5f * Time.deltaTime);
        }

        public class PointPair
        {
            public Vector3 start;
            public Vector3 end;

            public PointPair(Vector3 s, Vector3 e)
            {
                start = s;
                end = e;
            }
        }

        public class PointPairProcessor
        {
            public Vector3 start;
            public Vector3 end;
            public Vector3 result;
            public bool isDirty = false;

            public PointPairProcessor(Vector3 s, Vector3 e)
            {
                start = s;
                end = e;
            }
        }

        public enum NodeType
        {
            Void,
            Structure
        }

        public class LibGenLocator
        {
            public int? id;
            public int x;
            public int y;
            public NodeType nodetype;

            public LibGenLocator(int xx, int yy, int myId, NodeType _ntype)
            {
                x = xx;
                y = yy;
                id = myId;
                nodetype = _ntype;
            }

            public LibGenLocator(int xx, int yy, NodeType _ntype)
            {
                x = xx;
                y = yy;
                id = null;
                nodetype = _ntype;
            }
        }

        public class DepthMatrix
        {
            public List<LibGenLocator> nodes = new List<LibGenLocator>();
            public int floors;
            public DepthMatrix(int _floors)
            {
                floors = _floors;
            }
        }


        public List<LibGenLocator> currentVoid = new List<LibGenLocator>();
        public List<LibGenLocator> currentShores = new List<LibGenLocator>();
        void StartFindContigVoid(DepthMatrix dm)
        {
            //first void if any
            var currentNode = dm.nodes.FirstOrDefault(item => item.nodetype == NodeType.Void);
            //Get neighboring voids within 1 unit in cardinal directions.
            var nbs = dm.nodes.Where(
                item => item.nodetype == NodeType.Void &&
                //item.x == i |
                Math.Abs(item.x - currentNode.x) == 1 |
                //item.y == j |
                Math.Abs(item.y - currentNode.y) == 1
                ).Select(myitem => myitem).ToList();

            currentVoid.Clear();
            currentVoid.AddRange(nbs);
            FindContigVoid(dm);
        }
        
            void FindContigVoid(DepthMatrix dm)
            {
                var updates = 0;
                foreach(LibGenLocator lib in currentVoid.ToList())
                {
                    var nbs = dm.nodes.Where(
                    item => item.nodetype == NodeType.Void &&
                    //item.x == lib.x |
                    Math.Abs(item.x - lib.x) == 1 |
                    //item.y == lib.y |
                    Math.Abs(item.y - lib.y) == 1
                    ).Select(myitem => myitem).ToList();

                    foreach(LibGenLocator voidlib in nbs)
                    {
                        //check for duplicates by comparing coords
                        if(currentVoid.FirstOrDefault(item => item.x == voidlib.x & item.y == voidlib.y) == null)
                        {
                            currentVoid.Add(voidlib);
                            updates++;
                        }
                    }
                }
                //as long as we're finding more new voids, keep recursion going.
                if(updates > 0)
                {
                    FindContigVoid(dm);
                }
                //if not, we're done and we need to go to the next step and find the buildings on the shore of the void.
                else
                {
                    FindShoreNodes(dm);
                }
            }
            //We should end up with a 'ring' of structures around a void. We can more reliably pair up buildings from this shore for cables.
            void FindShoreNodes(DepthMatrix dm)
            {
                foreach (LibGenLocator lib in currentVoid)
                {
                    var nbs = dm.nodes.Where(
                    item => item.nodetype == NodeType.Structure &&
                    //item.x == lib.x |
                    Math.Abs(item.x - lib.x) == 1 |
                    //item.y == lib.y |
                    Math.Abs(item.y - lib.y) == 1
                    ).Select(myitem => myitem).ToList();

                    foreach (LibGenLocator voidlib in nbs)
                    {
                        //check for duplicates by comparing coords
                        if (currentShores.FirstOrDefault(item => item.x == voidlib.x & item.y == voidlib.y) == null)
                        {
                            currentShores.Add(voidlib);
                        }
                    }
                }
            }

        //Take a locator object and grab the actual building. This is getting a little loose.
        public LibraryGenerator performLookup(LibGenLocator lib)
        {
            //LibraryGenerator lg = new LibraryGenerator();
            LibraryGenerator lg = bldgs.FirstOrDefault(item => item.blockCoordinates[0] == lib.x & item.blockCoordinates[1] == lib.y) as LibraryGenerator;
            return lg;
        }

        public Vector3[] updateFloorHeight(Vector3[] centers, int floors)
        {
            List<Vector3> points = new List<Vector3>();
            foreach(Vector3 center in centers)
            {
                points.Add(center + Vector3.up * ((floors-1) * Settings.baseFloorHeight));
            }
            return points.ToArray();
        }

        /// <summary>
        /// Resolve overlaps in a single set of objects, taking care to remove only one member of each overlapped pair.
        /// </summary>
        /// <param name="set"></param>
        public static void checkOverlap(List<GameObject> set)
        {
            List<GameObject> toGo = new List<GameObject>();
            List<GameObject> toKeep = new List<GameObject>();
            foreach (GameObject go in set)
            {
                Bounds goBounds = go.GetComponent<Renderer>().bounds;
                foreach (GameObject othergo in set)
                {
                    Bounds otherGoBounds = othergo.GetComponent<Renderer>().bounds;
                    if (otherGoBounds.Intersects(goBounds))
                    {
                        toGo.Add(othergo);
                        toKeep.Add(go);
                        break;
                    }
                }
            }
            var final = toKeep.Except(toGo).ToList();
            Debug.Log("Removing " + final.Count + " overlapping structures.");
            foreach (GameObject overlapper in final) { Destroy(overlapper); }
        }

        /// <summary>
        /// Resolve overlaps, giving priority to <primary>.
        /// </summary>
        /// <param name="primary"></param>
        /// <param name="secondary"></param>
        public static void checkOverlap(List<GameObject> primary, List<GameObject> secondary)
        {
            List<GameObject> toGo = new List<GameObject>();
            if (primary == null | secondary == null)
            {
                Debug.Log("Nothing to remove.");
                return;
            }
            foreach (GameObject go in primary)
            {
                Bounds goBounds = go.GetComponent<Renderer>().bounds;
                foreach (GameObject othergo in secondary)
                {
                    Bounds otherGoBounds = othergo.GetComponent<Renderer>().bounds;
                    if (otherGoBounds.Intersects(goBounds))
                    {
                        toGo.Add(othergo);
                        break;
                    }
                }
            }

            Debug.Log("Removing " + toGo.Count + " priority 2 overlapping structures.");
            foreach (GameObject overlapper in toGo) { Destroy(overlapper); }
        }

        /// <summary>
        /// Work with the array of generated buildings and floor numbers to find clear paths for cables to be strung.
        /// </summary>
        /// <returns></returns>
        public List<Vector3> compromisedCables(int i, int j)
        {
            var points = new List<Vector3>();
            var cableDensity = UnityEngine.Random.Range(Settings.cableDensityLB, Settings.cableDensityUB);
            //Get the two values we're generating depth maps between
            var determineMaxMin = bldgs.OrderByDescending(libr => libr.floorManifest.Count).ToList();
            var tallestHeight = determineMaxMin[0].floorManifest.Count;
            var shortestStructure = determineMaxMin[determineMaxMin.Count - 1].floorManifest.Count;
            //Minimum height of 3 so we don't get cables dangling way down
            var shortestHeight = (shortestStructure > 3) ? shortestStructure : 3;
            var depthMats = new List<DepthMatrix>();

            //Build depth maps by breaking down all buildings according to height.
            //This depth map will allow us to determine exact spaces to occupy with cables.
            for(int level = shortestHeight; level < tallestHeight; level++)
            {
                var currentDMatrix = new DepthMatrix(level);
                for (int k = 0; k < i; k++)
                {
                    for (int l = 0; l < j; l++)
                    {
                        var possibleStructure = bldgs.FirstOrDefault(item => item.blockCoordinates[0] == k & item.blockCoordinates[1] == l);
                        if (possibleStructure != null && possibleStructure.floorManifest.Count >= level)
                        {
                            var wrapper = new LibGenLocator(k, l, possibleStructure.id, NodeType.Structure);
                            currentDMatrix.nodes.Add(wrapper);
                        }
                        else currentDMatrix.nodes.Add(new LibGenLocator(k, l, NodeType.Void));
                    }
                }
                depthMats.Add(currentDMatrix);
            }

            foreach(DepthMatrix dm in depthMats)
            {
                //Rehydrate the libraries we want to target for interbuilding features
                List<LibraryGenerator> actualLibs = new List<LibraryGenerator>();
                StartFindContigVoid(dm);
                foreach (LibGenLocator lgl in currentShores)
                {
                    actualLibs.Add(performLookup(lgl));
                }
                if(actualLibs.Count > 1)
                {
                    foreach(LibraryGenerator libgen in actualLibs)
                    {
                        //Find a partner more than 2 units away
                        var partner = actualLibs.FirstOrDefault(
                            item => 
                            Math.Abs(item.blockCoordinates[0] - libgen.blockCoordinates[0]) == 2 | 
                            Math.Abs(item.blockCoordinates[0] - libgen.blockCoordinates[0]) == 3 & 
                            Math.Abs(item.blockCoordinates[1] - libgen.blockCoordinates[1]) == 2 | 
                            Math.Abs(item.blockCoordinates[1] - libgen.blockCoordinates[1]) == 3) as LibraryGenerator;
                        
                        if (partner != null)
                        {
                            //If we find a viable partner, there's still the matter of determining if the node between is a structure.
                            var betweenCoords = BldgUtils.midpoint(partner.blockCoordinates, libgen.blockCoordinates);
                            var betweenTest = dm.nodes.FirstOrDefault(
                                item =>
                                item.x == betweenCoords[0] &
                                item.y == betweenCoords[1]);

                            if(betweenTest != null & betweenTest.nodetype != NodeType.Structure & RandomE.Chance(cableDensity))
                            {
                                var libgenStartFloor = UnityEngine.Random.Range(libgen.floorManifest.Count / 2, libgen.floorManifest.Count);
                                var partnerStartFloor = UnityEngine.Random.Range(partner.floorManifest.Count / 2, partner.floorManifest.Count);
                                MeshDraft cables = new MeshDraft();
                                GameObject ca = Instantiate(Resources.Load("CableHost")) as GameObject;
                                var cg = new CableGenerator(
                                        BldgUtils.compromisePoints(
                                        libgen.floorManifest[libgenStartFloor].pillarCenters,
                                        partner.floorManifest[partnerStartFloor].pillarCenters),
                                    UnityEngine.Random.Range(slackLB, slackUB), 20, UnityEngine.Random.Range(cableSizeLB, cableSizeUB));
                                cables.Add(cg._cable);
                                ca.name = cg._angleName;
                                ca.GetComponent<MeshFilter>().mesh = cables.ToMesh();
                                ca.GetComponent<MeshRenderer>().materials = _materials;
                            }
                        }
                    }
                    
                }
                currentShores.Clear();
            }

            return points;
        }

        public void CreateSurrounding(int i, int j)
        {
            var overlap = 0.8f;
            var doubleoverlap = overlap * 2;
            var md = new MeshDraft();
            //these dimensions are exactly 1 building floorplan too big
            var totalx = (i * Settings.baseFloorSize.x) + Settings.margin; //'short' dim
            var totalz = (j * Settings.baseFloorSize.z) + Settings.margin; //'long' dim

            var draftTop = MeshE.PlaneDraft(totalx + doubleoverlap, totalz + doubleoverlap, i * Settings.grottoResolution, j * Settings.grottoResolution);
            var draftNorth = MeshE.PlaneDraft(totalx + doubleoverlap, totalx + doubleoverlap, i * Settings.grottoResolution, i * Settings.grottoResolution);
            var draftSouth = MeshE.PlaneDraft(totalx + doubleoverlap, totalx + doubleoverlap, i * Settings.grottoResolution, i * Settings.grottoResolution);
            var draftEast =  MeshE.PlaneDraft(totalx + doubleoverlap, totalz + doubleoverlap, i * Settings.grottoResolution, j * Settings.grottoResolution);
            var draftWest =  MeshE.PlaneDraft(totalx + doubleoverlap, totalz + doubleoverlap, i * Settings.grottoResolution, j * Settings.grottoResolution);

            draftTop.Move(Vector3.up * (totalx - overlap));
            draftTop.FlipFaces();
            draftNorth.Rotate(Quaternion.Euler(90, 0, 0));
            draftNorth.Move(Vector3.up * totalx);
            draftNorth.Move(Vector3.forward * overlap);
            draftSouth.Rotate(Quaternion.Euler(90, 0, 0));
            draftSouth.Move(Vector3.up * totalx);
            draftSouth.Move(Vector3.forward * (totalz - overlap));
            draftSouth.FlipFaces();

            draftEast.Rotate(Quaternion.Euler(0, 0, 90));
            draftEast.Move(Vector3.right * (totalx - overlap));
            draftWest.Rotate(Quaternion.Euler(0, 0, 90));
            draftWest.Move(Vector3.right * overlap);
            draftWest.FlipFaces();

            var citystart = new Vector3(Settings.margin - (Settings.baseFloorSize.x/2), 0, Settings.margin - (Settings.baseFloorSize.z / 2));
            citycenter = new Vector3((Settings.margin + (Settings.baseFloorSize.x * (i/2))), -0.5f, (Settings.margin + (Settings.baseFloorSize.z * (j / 2))));
            var cityend = new Vector3((Settings.margin + totalz), 0, (Settings.margin + totalx));

            md.Add(draftTop);
            md.Add(draftNorth);
            md.Add(draftSouth);
            md.Add(draftEast);
            md.Add(draftWest);

            GameObject ca = Instantiate(Resources.Load("CatwalkHost")) as GameObject;
            ca.GetComponent<MeshFilter>().mesh = md.ToMesh();
            ca.GetComponent<MeshRenderer>().materials = _cavewalls;
            citystart = new Vector3(7, 0, 6);
            ca.transform.position = citystart;
            TightenGrotto(ca, citycenter);
        }

        public void PlayerToOrigin()
        {
            var destination = new Vector3(citycenter.x, Settings.baseFloorSize.y, citycenter.z);
            var player = GameObject.FindGameObjectWithTag("Player");
            player.transform.position = destination;
        }

        public void TightenGrotto(GameObject go, Vector3 startcorner)
        {
            var mesh = go.GetComponent<MeshFilter>().mesh;
            var verts = mesh.vertices;
            var newverts = new List<Vector3>();
            //var vertpairs = new List<PointPairProcessor>();
            

            for (int i =0; i < verts.Count(); i++)
            {             
                //cast a ray to the center of the city and move the vertex ~halfway toward it.
                var hit = new RaycastHit();
                if (Physics.Linecast(verts[i], startcorner, out hit))
                {
                    var randmod = UnityEngine.Random.Range(0.01f, 0.15f);
                    var divisor = baseGrottoDivisor + randmod;
                    if (hit.collider.GetType() == typeof(BoxCollider))
                    {
                        var currdist = Vector3.Distance(verts[i], hit.point);
                        var newpoint = transform.position = Vector3.MoveTowards(verts[i], hit.point, (currdist/ divisor));
                        verts[i] = newpoint;
                        //vertpairs.Add(new PointPairProcessor(verts[i], hit.point));
                    }
                    else if (hit.collider.GetType() == typeof(TerrainCollider))
                    {
                        var currdist = Vector3.Distance(verts[i], hit.point);
                        var newpoint = transform.position = Vector3.MoveTowards(verts[i], hit.point, (currdist / (divisor + 1)));
                        verts[i] = newpoint;
                    }
                    else if (hit.collider.GetType() == typeof(MeshCollider))
                    {
                        var currdist = Vector3.Distance(verts[i], hit.point);
                        var newpoint = transform.position = Vector3.MoveTowards(verts[i], hit.point, (currdist / baseGrottoDivisor));
                        verts[i] = newpoint;
                    }
                }
                newverts.Add(verts[i]);
            }
            mesh.vertices = newverts.ToArray();
            //mesh.vertices = SmoothGrotto(vertpairs, splitTriangles(mesh.triangles));
            go.GetComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().mesh;
            //drop the mesh down to push artifacts that occur on its lower edge out of view
            go.transform.position = new Vector3(go.transform.position.x, go.transform.position.y - 1.5f, go.transform.position.z);
            BldgUtils.Flatten(go);
        }

        //Compose sets of vertices that correspond to triangles in a two-dimensional way.
        public List<int[]> splitTriangles(int[] triangles)
        {
            var trisets = new List<int[]>();
            var tricount = triangles.Count() / 3;
            int j = 0;
            for (int i = 0; i < tricount; i++)
            {
                var currtri = new int[3];
                currtri[0] = triangles[j];
                j++;
                currtri[1] = triangles[j];
                j++;
                currtri[2] = triangles[j];
                j++;
                trisets.Add(currtri);
            }
            return trisets;
        }

        /// <summary>
        /// Simple, average-based smoothing on a per-triangle basis.
        /// </summary>
        /// <param name="pairs"></param>
        /// <param name="tris"></param>
        /// <returns></returns>
        public Vector3[] SmoothGrotto(List<PointPairProcessor> pairs, List<int[]> tris)
        {
            var newverts = new List<Vector3>();
            //Average distortion per triangle
            foreach(int[] tri in tris)
            {
                float[] currdists = new float[3];

                for(int i = 0; i < tri.Count(); i++)
                {
                    var currdist = Vector3.Distance(pairs[tri[i]].start, pairs[tri[i]].end);
                    currdists[i] = currdist;
                }
                var topdistortion = currdists.Max();
                var arravg = currdists.Sum() / 3;
                float magicfactor = arravg / topdistortion + 1;

                for (int j = 0; j < tri.Count(); j++)
                {
                    if (!pairs[tri[j]].isDirty)
                    {
                        pairs[tri[j]].result = new Vector3(
                            (pairs[tri[j]].start.x + pairs[tri[j]].end.x) / magicfactor,
                            (pairs[tri[j]].start.y + pairs[tri[j]].end.y) / magicfactor,
                            (pairs[tri[j]].start.z + pairs[tri[j]].end.z) / magicfactor);
                        pairs[tri[j]].isDirty = true;
                    }
                }

            }

            foreach(PointPairProcessor finalpair in pairs)
            {
                newverts.Add(finalpair.result);
            }
            return newverts.ToArray();
        }

        //For every building that needs a fire escape, check its neighbors for interference.
        public List<GameObject> applyFireEscapes()
        {
            var actualEscapes = new List<GameObject>();
            foreach (LibraryGenerator lib in bldgs)
            {
                if (lib.gapSets.Count > 0)
                {
                    var possibleMountingFaces = new List<Direction>();
                    var neighbors = new List<LibraryGenerator>();
                    //To completely resolve overlap issue, we could get all possible neighbors here, even diagonals.
                    var northneighbor = bldgs.FirstOrDefault(item => 
                        (item.blockCoordinates[0] == (lib.blockCoordinates[0] + 1)) & 
                        (item.blockCoordinates[1] == lib.blockCoordinates[1])
                    );
                    if (northneighbor != null) neighbors.Add(northneighbor);

                    var southneighbor = bldgs.FirstOrDefault(item => 
                        (item.blockCoordinates[0] == (lib.blockCoordinates[0] - 1)) & 
                        (item.blockCoordinates[1] == lib.blockCoordinates[1])
                    );
                    if (southneighbor != null) neighbors.Add(southneighbor);

                    var eastneighbor = bldgs.FirstOrDefault(item => 
                        (item.blockCoordinates[0] == lib.blockCoordinates[0]) & 
                        (item.blockCoordinates[1] == (lib.blockCoordinates[1] +1))
                    );
                    if (eastneighbor != null) neighbors.Add(eastneighbor);

                    var westneighbor = bldgs.FirstOrDefault(item => 
                        (item.blockCoordinates[0] == lib.blockCoordinates[0]) & 
                        (item.blockCoordinates[1] == (lib.blockCoordinates[1] -1))
                    );
                    if (westneighbor != null) neighbors.Add(westneighbor);

                    //Figure out which, if any, neighbors are short enough to give the fire escape room to exist.
                    foreach (LibraryGenerator knownNeighbor in neighbors)
                    {
                        if (knownNeighbor.floorManifest.Count < lib.gapSets[0][0])
                        {
                            possibleMountingFaces.Add(BldgUtils.dirFrom2dCoords(lib.blockCoordinates, knownNeighbor.blockCoordinates));
                        }
                    }

                    var direction = 0;
                    if(possibleMountingFaces.Count > 0)
                    {
                        System.Random rnd = new System.Random();
                        if (possibleMountingFaces.Count > 1) direction = rnd.Next(0, possibleMountingFaces.Count);
                        var mountDirection = possibleMountingFaces[direction];

                        MeshDraft fireEscapes = new MeshDraft();
                        GameObject ca = Instantiate(Resources.Load("CatwalkHost")) as GameObject;
                        foreach(List<int> gapset in lib.gapSets)
                        {
                            fireEscapes.Add(new FireEscapeGenerator(lib.floorManifest, gapset, mountDirection)._fireEscape);
                            ca.name = mountDirection.ToString();
                            ca.GetComponent<MeshFilter>().mesh = fireEscapes.ToMesh();
                            ca.GetComponent<MeshRenderer>().materials = _markerMaterials;
                            ca.GetComponent<MeshCollider>().sharedMesh = ca.GetComponent<MeshFilter>().mesh;
                            actualEscapes.Add(ca);
                        }
                    }
                }
            }

            checkOverlap(actualEscapes);
            return actualEscapes;
        }

        public void organizeBuildings(int i, int j)
        {
            librows.Clear();
            libcols.Clear();
            for (int h = 0; h < i; h++)
            {
                var currentRow = (bldgs.Where(item => item.blockCoordinates[0] == h).Select(myitem => myitem)).ToList();
                if (currentRow.Count > 1) librows.Add(currentRow);
            }

            for (int v = 0; v < j; v++)
            {
                var currentColumn = (bldgs.Where(item => item.blockCoordinates[1] == v).Select(myitem => myitem)).ToList();
                if (currentColumn.Count > 1) libcols.Add(currentColumn);
            }
        }

        //Catwalks are pretty rare. This should be looked at again.
        public List<GameObject> Catwalks(int i, int j)
        {
            int cwcount = 0;
            var gos = new List<GameObject>();
            gos.AddRange(pickCatwalks(librows, Orientation.X, 1));
            gos.AddRange(pickCatwalks(libcols, Orientation.Z, 0));
            cwcount += gos.Count;
            return gos;
        }

        private List<GameObject> pickCatwalks(List<List<LibraryGenerator>> libsets, Orientation or, int coord)
        {
            var gos = new List<GameObject>();
            foreach (List<LibraryGenerator> libset in libsets)
            {
                var orderedLibs = libset.OrderByDescending(libr => libr.floorManifest.Count).ToList();

                var structureBounds = StructureHeightBounds(orderedLibs, coord);
                //Get the distance between the two highest structures in the row
                var peakdistance = structureBounds[1] - structureBounds[0];
                //If there's a gap, do the rest of the work.
                if (peakdistance > 1)
                {
                    var moat = new List<LibraryGenerator>();
                    var moatindices = Enumerable.Range(structureBounds[0], structureBounds[1]).ToArray();
                    foreach (int moatindex in moatindices)
                    {
                        var moatbldg = libset.FirstOrDefault(libr => libr.blockCoordinates[coord] == moatindex);
                        if (moatbldg != null)
                        {
                            moat.Add(moatbldg);
                        }
                    }
                    var orderedBetween = moat.OrderByDescending(libr => libr.floorManifest.Count).ToList();
                    orderedBetween.RemoveAt(0);
                    if (orderedBetween.Count > 0)
                    {
                        var dupecheck = orderedBetween.FindIndex(item => item.floorManifest.Count == orderedLibs[1].floorManifest.Count);
                        //Check to see if any of the buildings between peaks have a height equal to our smallest peak.
                        if (dupecheck < 0)
                        {
                            //Determine at random the desired height of the catwalk. Lower bound is set to smallest building in space between peaks.
                            var lowestheight = orderedBetween[0].floorManifest.Count;
                            var catwalkLevel = UnityEngine.Random.Range(orderedLibs[1].floorManifest.Count, lowestheight) - 1;

                            var startend = BldgUtils.compromisePoints(orderedLibs[0].floorManifest[catwalkLevel].sideCenters, orderedLibs[1].floorManifest[catwalkLevel].sideCenters);

                            MeshDraft catwalks = new MeshDraft();
                            GameObject ca = Instantiate(Resources.Load("CatwalkHost")) as GameObject;
                            catwalks.Add(new CatwalkGenerator(ca.transform.TransformPoint(startend[0]), ca.transform.TransformPoint(startend[1]), catwalkLevel, or)._catwalk);

                            ca.GetComponent<MeshFilter>().mesh = catwalks.ToMesh();
                            ca.GetComponent<MeshRenderer>().materials = _materials;
                            ca.GetComponent<MeshCollider>().sharedMesh = ca.GetComponent<MeshFilter>().mesh;
                            gos.Add(ca);
                        }
                    }
                }
            }
            return gos;
        }

        /// <summary>
        /// Returns the zero-based grid coordinates of the second highest and highest buildings in a set.
        /// </summary>
        /// <param name="libs"></param>
        /// <param name="axis"></param>
        /// <returns></returns>
        private int[] StructureHeightBounds(List<LibraryGenerator> libs, int axis)
        {
            var bounds = new int[2];
            //Where the highest two buildings are located in the column by index
            var highest = libs[0].blockCoordinates[axis];
            var secondhighest = libs[1].blockCoordinates[axis];
            bounds[0] = ((highest < secondhighest) ? highest : secondhighest);
            bounds[1] = ((highest > secondhighest) ? highest : secondhighest);

            return bounds;
        }

        private MeshDraft TestStackedWall(Vector3 center)
        {
            return BldgUtils.StackedWall(center, Settings.baseFloorSize.x, Settings.baseFloorHeight, 0.05f, 40.0f, Tendency.Falling);
        }

        void Generate()
        {
            var extantbldgs = GameObject.FindGameObjectsWithTag("ProcStructure");
            foreach(GameObject structure in extantbldgs)
            {
                Destroy(structure);
            }
            bldgs.Clear();

            var extantcables = GameObject.FindGameObjectsWithTag("Cables");
            foreach (GameObject cable in extantcables)
            {
                Destroy(cable);
            }

            var extantcatwalks = GameObject.FindGameObjectsWithTag("Catwalks");
            foreach (GameObject catwalk in extantcatwalks)
            {
                Destroy(catwalk);
            }

            var extanttorches = GameObject.FindGameObjectsWithTag("Torches");
            foreach (GameObject torch in extanttorches)
            {
                Destroy(torch);
            }

            var extantglass = GameObject.FindGameObjectsWithTag("Glass");
            foreach (GameObject glass in extantglass)
            {
                Destroy(glass);
            }

            //Generating buildings
            float xOffset = Settings.baseFloorSize.x;
            float zOffset = Settings.baseFloorSize.z;
            int i, j = 0;
            //Maximum structures that can fit on the map's x axis.
            maxStructuresX = Settings.mapSizeX / (int)Settings.baseFloorSize.x;

            //Based on total map size, how many we should fit into a given row.
            maxStructuresZ = Settings.mapSizeZ / (int)Settings.baseFloorSize.z;

            int structureId = 0;
            for(i =0; i<maxStructuresX; i++)
            {
                for(j=0;j<maxStructuresZ; j++)
                {
                    if (RandomE.Chance(0.8f))
                    {
                        GameObject go = Instantiate(Resources.Load("ProcLibrary")) as GameObject;
                        Vector3 pos = new Vector3(Settings.margin + (i * xOffset), 0, Settings.margin + (j * zOffset));
                        go.transform.position = pos;
                        var currBldg = new LibraryGenerator(go);
                        currBldg.blockCoordinates = new int[] { i, j };
                        currBldg.id = structureId;
                        structureId++;

                        if(currBldg.Windows != null) {
                            GameObject wins = Instantiate(Resources.Load("StructureWindows")) as GameObject;
                            wins.GetComponent<MeshFilter>().mesh = currBldg.Windows.ToMesh();
                            wins.GetComponent<MeshRenderer>().materials = _transMaterials;
                            wins.GetComponent<MeshCollider>().sharedMesh = wins.GetComponent<MeshFilter>().mesh;
                            wins.transform.position = pos;
                        }
                                               
                        go.GetComponent<MeshFilter>().mesh = currBldg.Building.ToMesh();
                        go.GetComponent<MeshRenderer>().materials = _materials;
                        go.GetComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().mesh;
                        go.GetComponent<BoxCollider>().size = new Vector3(Settings.baseFloorSize.x + 0.005f, Settings.baseFloorHeight * currBldg.floorManifest.Count, Settings.baseFloorSize.z + 0.005f);
                        go.GetComponent<BoxCollider>().center = new Vector3(0, Settings.baseFloorHeight * (currBldg.floorManifest.Count/2), 0);
                        go.SetActive(false);
                        go.SetActive(true);
                        if (RandomE.Chance(0.25f) & currBldg.floorManifest.Count > 4)
                        {
                            //GameObject _torch = Instantiate(Resources.Load("Torch")) as GameObject;
                            //Vector3 torchPosition = currBldg.lightpoint;
                            //_torch.transform.position = torchPosition;
                           
                        }
                        bldgs.Add(currBldg);
                    }
                }
            }
            organizeBuildings(i, j);

            checkOverlap(Catwalks(i, j), applyFireEscapes());

            compromisedCables(i, j);

            var finishedbldgs = GameObject.FindGameObjectsWithTag("ProcStructure");
            foreach (GameObject structure in finishedbldgs)
            {
                structure.GetComponent<MeshCollider>().enabled = false;
            }

            CreateSurrounding(i, j);

            
            foreach (GameObject structure in finishedbldgs)
            {
                structure.GetComponent<MeshCollider>().enabled = true;
                structure.GetComponent<BoxCollider>().enabled = false;
            }

            PlayerToOrigin();
            //GameObject sw = Instantiate(Resources.Load("ProcLibrary")) as GameObject;
            //sw.GetComponent<MeshFilter>().mesh = TestStackedWall(new Vector3(0f,0f,0f)).ToMesh();
            //sw.GetComponent<MeshRenderer>().materials = _materials;
        }
    }
}
