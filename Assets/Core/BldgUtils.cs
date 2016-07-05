using System;
using UnityEngine;
using System.Collections;
using ProceduralToolkit;
using System.Collections.Generic;
using Assets.Core;
using System.Linq;
namespace Assets.Core
{
    public enum Direction
    {
        North,
        South,
        East,
        West
    }
    public enum FloorType
    {
        Null,
        None,
        Atrium,
        Partial,
        PartialAtrium,
        Full,
        Access,
        PartialAccess
    };
    public enum BldgType
    {
        Library,
        None
    }
    public enum Tendency
    {
        Rising,
        Falling,
        Ragged,
        Serrated
    }
    public class FloorInfo
    {
        public FloorType fType;
        public bool Traversable;
        public Vector3 dimensions;
        public Vector3[] sideCenters;
        public Vector3[] pillarCenters;
        public Direction direction;
        public FloorInfo(FloorType ft, bool trav, Vector3 dims, Direction dir, Vector3[] sc, Vector3[] pc)
        {
            fType = ft;
            Traversable = trav;
            dimensions = dims;
            direction = dir;
            sideCenters = sc;
            pillarCenters = pc;
        }
    }
    public class LibraryManifest
    {
        public List<FloorInfo> Floors;
        public int id;
        public List<Vector3> wayPoints;
        public BldgType bType;
        public LibraryManifest(List<FloorInfo> _floors, int _id, List<Vector3> _wps, BldgType _type)
        {
            Floors = _floors;
            id = _id;
            wayPoints = _wps;
            bType = _type;
        }
        void internalGenerate()
        {
        }
    }
    public class wayPointComparator
    {
        public float distance;
        public int itemi;
        public int itemj;
        public wayPointComparator(float dist, int ii, int ij)
        {
            distance = dist;
            itemi = ii;
            itemj = ij;
        }
    }
    public class FloorPortOffset
    {
        public Direction direction;
        public Vector3 dimensions;
        public float PortLength;
        public float PortWidth;
        public float PortOffsetX;
        public float PortOffsetZ;
        public Vector3 center;
        public float XLongSide;
        public float XShortSide;
        public float XLongSideMove;
        public float XShortSideMove;
        public float ZLongSide;
        public float ZShortSide;
        public float ZLongSideMove;
        public float ZShortSideMove;
        public float RampOffsetX;
        public float RampOffsetZ;
        public List<MeshDraft> pieces = new List<MeshDraft>();
        /// <summary>
        /// Returns a set of perfectly-interlocking floor pieces with no overlap. Comes with a ramp.
        /// </summary>
        /// <param name="dir"></param>
        /// <param name="dim"></param>
        /// <param name="portLength"></param>
        /// <param name="portWidth"></param>
        /// <param name="portOffsetX"></param>
        /// <param name="portOffsetZ"></param>
        /// <param name="_center"></param>
        public FloorPortOffset(Direction dir, Vector3 dim, float portLength, float portWidth, float portOffsetX, float portOffsetZ, Vector3 _center)
        {
            direction = dir;
            dimensions = dim;
            center = _center;
            PortLength = portLength;
            PortWidth = portWidth;
            PortOffsetX = portOffsetX;
            PortOffsetZ = portOffsetZ;
            var side1dir = Vector3.forward;
            var side2dir = Vector3.back;
            var side3dir = Vector3.right;
            var side4dir = Vector3.left;
            var keyOffset = (dim.z / 2) - portOffsetZ;
            XLongSide = (dim.x - PortOffsetX) - (PortLength / 2);
            XShortSide = PortOffsetX - (PortLength / 2);
            XLongSideMove = (dim.x - XLongSide) / 2;
            XShortSideMove = (dim.x - XShortSide) / 2;
            ZLongSide = (dim.z - PortOffsetZ) - (PortWidth / 2);
            ZShortSide = PortOffsetZ - (PortWidth / 2); ;
            ZLongSideMove = (dim.z - ZLongSide) / 2;
            ZShortSideMove = (dim.z - ZShortSide) / 2;
            var side1 = MeshE.HexahedronDraft(dim.x, ZLongSide, dim.y);
            var side2 = MeshE.HexahedronDraft(dim.x, ZShortSide, dim.y);
            var side3 = MeshE.HexahedronDraft(XLongSide, PortWidth, dim.y);
            var side4 = MeshE.HexahedronDraft(XShortSide, PortWidth, dim.y);
            if (direction == Direction.West | direction == Direction.North)
            {
                RampOffsetX = -(((dim.z - PortOffsetZ) - (dim.z / 2)));
                RampOffsetZ = -((dim.x - PortOffsetX) - (dim.x / 2));
            }
            else
            {
                side1dir = Vector3.back;
                side2dir = Vector3.forward;
                side3dir = Vector3.left;
                side4dir = Vector3.right;
                RampOffsetX = ((dim.z - PortOffsetZ) - (dim.z / 2));
                RampOffsetZ = ((dim.x - PortOffsetX) - (dim.x / 2));
            }
            side1.Move(side1dir * ZLongSideMove);
            side2.Move(side2dir * ZShortSideMove);
            side3.Move(side3dir * XLongSideMove);
            side4.Move(side4dir * XShortSideMove);
            side3.Move(side2dir * keyOffset);
            side4.Move(side2dir * keyOffset);
            side1.Move(center + Vector3.up * dim.y / 2);
            side2.Move(center + Vector3.up * dim.y / 2);
            side3.Move(center + Vector3.up * dim.y / 2);
            side4.Move(center + Vector3.up * dim.y / 2);
            pieces.Add(side1);
            pieces.Add(side2);
            pieces.Add(side3);
            pieces.Add(side4);
            var r = BldgUtils.ShortRamp(dim.y, center, dir);
            r.Move(Vector3.forward * RampOffsetX);
            r.Move(Vector3.right * RampOffsetZ);
            pieces.Add(r);
        }
    }
    public static class BldgUtils
    {
        
        public static T Next<T>(this T src) where T : struct
        {
            if (!typeof(T).IsEnum) throw new ArgumentException(String.Format("Argument {0} is not an Enum", typeof(T).FullName));
            T[] Arr = (T[])Enum.GetValues(src.GetType());
            int j = Array.IndexOf<T>(Arr, src) + 1;
            return (Arr.Length == j) ? Arr[0] : Arr[j];
        }
        /// <summary>
        /// This is meant to process a mesh into a disjointed one that will shade normals separately with no smoothing.
        /// </summary>
        /// <param name="current"></param>
        public static void Flatten(GameObject current)
        {
            MeshFilter mf;
            // Create the duplicate game object
            //GameObject go = Instantiate(current) as GameObject;
            mf = current.GetComponent<MeshFilter>();
            Mesh mesh = mf.mesh;
            //Process the triangles
            Vector3[] oldVerts = mesh.vertices;
            Debug.Log("Old Verts Count:" + oldVerts.Count() + "Old Tris Count:" + mesh.triangles.Count());
            int[] triangles = mesh.triangles;
            Vector3[] vertices = new Vector3[triangles.Length];
            for (int i = 0; i < triangles.Length; i++)
            {
                vertices[i] = oldVerts[triangles[i]];
                triangles[i] = i;
            }
            mesh.vertices = vertices;
            Debug.Log("New Verts Count:" + vertices.Count() + "New Tris Count:" + triangles.Count());
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }
        /// <summary>
        /// Generates a flat floor with a large open gap in the middle. AtriumSize is a ratio expressed as a decimal.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        /// <param name="height"></param>
        /// <param name="atriumSize"></param>
        /// <returns></returns>
        public static MeshDraft[] AtriumFloor(Vector3 center, float width, float length, float height, float atriumSize, bool addRamp)
        {
            var _floor = new List<MeshDraft>();
            var watriumOffset = width * atriumSize;
            var latriumOffset = length * atriumSize;
            var wmagic = (width / 2) * atriumSize;
            var lmagic = (length / 2) * atriumSize;
            var side1 = MeshE.HexahedronDraft(width, (length - latriumOffset), height);
            var side2 = MeshE.HexahedronDraft(width, (length - latriumOffset), height);
            side1.Move(Vector3.forward * lmagic);
            side2.Move(Vector3.back * lmagic);
            //Side 3 and 4 are smaller and key into the negative space of 1 and 2 with no overlap.
            var side3 = MeshE.HexahedronDraft((width - watriumOffset), latriumOffset - (length - latriumOffset), height);
            var side4 = MeshE.HexahedronDraft((width - watriumOffset), latriumOffset - (length - latriumOffset), height);
            side3.Move(Vector3.left * wmagic);
            side4.Move(Vector3.right * wmagic);
            side1.Move(center + Vector3.up * height / 2);
            side2.Move(center + Vector3.up * height / 2);
            side3.Move(center + Vector3.up * height / 2);
            side4.Move(center + Vector3.up * height / 2);
            _floor.Add(side1);
            _floor.Add(side2);
            _floor.Add(side3);
            _floor.Add(side4);
            if (addRamp) _floor.Add(Ramp(length, width, latriumOffset, watriumOffset, height, center, false));
            return _floor.ToArray();
        }
        /// <summary>
        /// Returns four interlocking pieces of a facade with a port in the middle.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        /// <param name="thickness">Pillar thickness for the library that will receive this facade.</param>
        /// <param name="height">Height of floors for the library that will receive this facade.</param>
        /// <param name="portSize"></param>
        /// <param name="elevation"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static MeshDraft[] OpenFacade(Vector3 center, float thickness, float length, float width, float height, float portSize, float elevation, Direction dir)
        {
            float shortenedSizeRatio = portSize / 2;
            var myLength = 0.0f;
            var myThickness = 0.0f;
            var side3 = new MeshDraft();
            var side4 = new MeshDraft();
            if (dir == Direction.East | dir == Direction.West)
            {
                myLength = thickness;
                myThickness = (width - (thickness * 2));
            }
            else
            {
                myThickness = thickness;
                myLength = (length - (thickness * 2));
            }
            var _floor = new List<MeshDraft>();
            //the long sides
            var longsideheight = ((height - Settings.baseFloorSize.y) * portSize)/ 2;
            var side1 = MeshE.HexahedronDraft(myThickness, myLength, longsideheight);
            var side2 = MeshE.HexahedronDraft(myThickness, myLength, longsideheight);
            side1.Move(center + Vector3.down * ((height - Settings.baseFloorSize.y) / 2));
            float longtweak = (height / 2) - (longsideheight / 2);
            side1.Move(Vector3.down * longtweak);
            side2.Move(center + Vector3.down * ((height - Settings.baseFloorSize.y) / 2));
            side2.Move(Vector3.up * longtweak);

            //the short sides
            //TODO: double the port size modifier for the short size so the proportions are better.
            var shorttweak = 0.0f;
            if (dir == Direction.East | dir == Direction.West)
            {
                var shortsidewidth = (myThickness * shortenedSizeRatio) / 2;
                shorttweak = (myThickness / 2) - (shortsidewidth / 2);
                side3 = MeshE.HexahedronDraft(shortsidewidth, myLength, (height * portSize) + (Settings.baseFloorSize.y / 2));
                side4 = MeshE.HexahedronDraft(shortsidewidth, myLength, (height * portSize) + (Settings.baseFloorSize.y / 2));
                side3.Move(Vector3.left * shorttweak);
                side4.Move(Vector3.right * shorttweak);
            }
            else
            {
                var shortsidewidth = (myLength * shortenedSizeRatio) / 2;
                shorttweak = (myLength / 2) - (shortsidewidth / 2);
                side3 = MeshE.HexahedronDraft(myThickness, shortsidewidth, (height * portSize) + (Settings.baseFloorSize.y / 2));
                side4 = MeshE.HexahedronDraft(myThickness, shortsidewidth, (height * portSize) + (Settings.baseFloorSize.y / 2));
                side3.Move(Vector3.forward * shorttweak);
                side4.Move(Vector3.back * shorttweak);
            }
            side3.Move(center + Vector3.down * ((height - Settings.baseFloorSize.y) / 2));
            side4.Move(center + Vector3.down * ((height - Settings.baseFloorSize.y) / 2));

            _floor.Add(side1);
            _floor.Add(side2);
            _floor.Add(side3);
            _floor.Add(side4);

            return _floor.ToArray();
        }

        /// <summary>
        /// Puts a bar across the center of an open floor.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="thickness"></param>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="portSize"></param>
        /// <param name="elevation"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static MeshDraft BarFacade(Vector3 center, float thickness, float length, float width, float height, float portSize, float elevation, Direction dir)
        {
            var myLength = 0.0f;
            var myThickness = 0.0f;
            if (dir == Direction.East | dir == Direction.West)
            {
                myLength = thickness;
                myThickness = (width - (thickness * 2));
            }
            else
            {
                myThickness = thickness;
                myLength = (length - (thickness * 2));
            }
            var _floor = new List<MeshDraft>();
            //the long sides
            var longsideheight = ((height - Settings.baseFloorSize.y) * portSize) / 2;
            var bar = MeshE.HexahedronDraft(myThickness, myLength, longsideheight);
            return bar;
        }

        /// <summary>
        /// Returns four interlocking pieces of a facade with a port in the middle.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        /// <param name="thickness">Pillar thickness for the library that will receive this facade.</param>
        /// <param name="height">Height of floors for the library that will receive this facade.</param>
        /// <param name="portSize"></param>
        /// <param name="elevation"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static MeshDraft[] RailingFacade(Vector3 center, float thickness, float length, float width, float height, float portSize, float elevation, Direction dir)
        {

            var myLength = 0.0f;
            var myThickness = 0.0f;
            if (dir == Direction.East | dir == Direction.West)
            {
                myLength = thickness;
                myThickness = (width - (thickness * 2));
            }
            else
            {
                myThickness = thickness;
                myLength = (length - (thickness * 2));
            }
            var _floor = new List<MeshDraft>();
            var longsideheight = ((height - Settings.baseFloorSize.y) * portSize) / 2;
            var side1 = MeshE.HexahedronDraft(myThickness, myLength, longsideheight);
            //the short sides
            var shortsidewidth = (myLength * portSize) / 2;
            side1.Move(center + Vector3.down * ((height - Settings.baseFloorSize.y) / 2));
            float side1tweak = (height / 2) - (longsideheight / 2);
            side1.Move(Vector3.down * side1tweak);

            _floor.Add(side1);

            return _floor.ToArray();
        }
        /// <summary>
        /// Generates a flat floor with a large open gap in the middle. AtriumSize is a ratio expressed as a decimal.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        /// <param name="height"></param>
        /// <param name="atriumSize"></param>
        /// <returns></returns>
        public static MeshDraft[] AtriumFloor(Vector3 center, float width, float length, float height, float atriumSize, bool addRamp, Direction direction)
        {
            var _floor = new List<MeshDraft>();
            var watriumOffset = width * atriumSize;
            var latriumOffset = length * atriumSize;
            var wmagic = (width / 2) * atriumSize;
            var lmagic = (length / 2) * atriumSize;
            var side1 = MeshE.HexahedronDraft(width, (length - latriumOffset), height);
            var side2 = MeshE.HexahedronDraft(width, (length - latriumOffset), height);
            side1.Move(Vector3.forward * lmagic);
            side2.Move(Vector3.back * lmagic);
            //Side 3 and 4 are smaller and key into the negative space of 1 and 2 with no overlap.
            var side3 = MeshE.HexahedronDraft((width - watriumOffset), latriumOffset - (length - latriumOffset), height);
            var side4 = MeshE.HexahedronDraft((width - watriumOffset), latriumOffset - (length - latriumOffset), height);
            side3.Move(Vector3.left * wmagic);
            side4.Move(Vector3.right * wmagic);
            side1.Move(center + Vector3.up * height / 2);
            side2.Move(center + Vector3.up * height / 2);
            side3.Move(center + Vector3.up * height / 2);
            side4.Move(center + Vector3.up * height / 2);
            _floor.Add(side1);
            _floor.Add(side2);
            _floor.Add(side3);
            _floor.Add(side4);
            
            if (addRamp)
            {
                var ramp = new MeshDraft();
                if (direction == Direction.East | direction == Direction.West) ramp = Ramp(length, width + 1.0f, latriumOffset, watriumOffset, height, center, false);
                else
                {
                    ramp = Ramp(width, length + 1.0f, watriumOffset, latriumOffset, height, center, true);
                }
                _floor.Add(ramp);
            }
            return _floor.ToArray();
        }
        /// <summary>
        /// Reworked to use a unit-based offset value instead of a ratio
        /// </summary>
        /// <param name="center"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        /// <param name="height"></param>
        /// <param name="portCenterLong"></param>
        /// <returns></returns>
        public static MeshDraft[] AccessFloorUnitary(Vector3 center, Vector3 floordims, Direction dir)
        {
            var portWidth = 0.0f;
            var portLength = 0.0f;
            //Point the access port to be perpendicular to floor's long dimension.
            if (dir == Direction.West | dir == Direction.East)
            {
                portWidth = Settings.portSizeL;
                portLength = Settings.portSizeW;
            }
            else
            {
                portWidth = Settings.portSizeW;
                portLength = Settings.portSizeL;
            }
            return new FloorPortOffset(dir, floordims, portLength, portWidth, Settings.portOffsetX, Settings.portOffsetZ, center).pieces.ToArray();
        }
        /// <summary>
        /// This overload places the port offsets in a direction-sensitive manner. This is important for smaller floor sizes, like fire escapes. Less so for full floors.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="width"></param>
        /// <param name="length"></param>
        /// <param name="height"></param>
        /// <param name="portCenterLong"></param>
        /// <returns></returns>
        public static MeshDraft[] AccessFloorUnitary(Vector3 center, Vector3 floordims, Direction dir, float portOffsetX, float portOffsetZ)
        {
            var portWidth = 0.0f;
            var portLength = 0.0f;
            var portactualX = 0.0f;
            var portactualZ = 0.0f;
            //Point the access port to be perpendicular to floor's long dimension.
            if (dir == Direction.West | dir == Direction.East)
            {
                portWidth = Settings.portSizeL;
                portLength = Settings.portSizeW;
                portactualX = portOffsetX;
                portactualZ = portOffsetZ;
            }
            else
            {
                portWidth = Settings.portSizeW;
                portLength = Settings.portSizeL;
                portactualX = portOffsetZ;
                portactualZ = portOffsetX;
            }
            return new FloorPortOffset(dir, floordims, portLength, portWidth, portactualX, portactualZ, center).pieces.ToArray();
        }
        //A wall made from vertical posts with an uneven top edge.
        public static MeshDraft StackedWall(Vector3 center, float width, float oallHeight, float thickness, float resolution, Tendency tend)
        {
            var stepmodifier = 0.0f;
            switch (tend)
            {
                case Tendency.Falling:
                    stepmodifier = -0.05f;
                    break;
                case Tendency.Rising:
                    stepmodifier = +0.05f;
                    break;
                case Tendency.Ragged:
                    break;
                case Tendency.Serrated:
                    break;
            }
            var _wall = new MeshDraft();
            var startpoint = center + Vector3.back * (width/2);
            var stepwidth = width / resolution;
            var noiseOffset = new Vector2(UnityEngine.Random.Range(0f, 100f), UnityEngine.Random.Range(0f, 100f));
            for (int i = 0; i < resolution; i++)
            {
                //var modifier = UnityEngine.Random.Range(0.05f, 0.3f);
                var noisemodifier = Mathf.PerlinNoise(i * 20.0f + noiseOffset.x, i * 8.0f + noiseOffset.y);
                var side4 = MeshE.HexahedronDraft(thickness, stepwidth, (oallHeight + noisemodifier + (stepmodifier * i)));
                side4.Move(Vector3.back * (width/2));
                side4.Move(Vector3.forward * (i * stepwidth));
                side4.Move(Vector3.up * (noisemodifier / 2));
                _wall.Add(side4);
            }
            return _wall;
        }
        public static MeshDraft Ramp(float length, float width, float latriumOffset, float watriumOffset, float height, Vector3 center, bool rotate90)
        {
            //Hypotenuse of right triangle formed by edge of atrium and height of floor.
            var ramplength = Mathf.Sqrt((Mathf.Pow((length - (latriumOffset * 2)), 2)) + (Mathf.Pow(Settings.baseFloorHeight, 2)));
            var sine = Settings.baseFloorHeight / ramplength;
            var rampangle = sine * Mathf.Rad2Deg;
            var ramp = MeshE.HexahedronDraft(((width - watriumOffset) / 2), ramplength, height);
            ramp.Rotate(Quaternion.Euler(rampangle, 0, 0));
            if(rotate90) ramp.Rotate(Quaternion.Euler(0, 90, 0));
            ramp.Move(center + Vector3.up * height / 2);
            ramp.Move(Vector3.down * Settings.baseFloorHeight / 2);
            return ramp;
        }
        /// <summary>
        /// Short, climbable access ramps for non-atrium floors.
        /// </summary>
        /// <param name="length"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="center"></param>
        /// <returns></returns>
        public static MeshDraft ShortRamp(float height, Vector3 center, Direction dir)
        {
            //Hardcoding ramplength for shorter ramps so we can prevent them from being unclimbable.
            var ramplength = Settings.baseFloorHeight * 2;
            var sine = (Settings.baseFloorHeight + (height)) / ramplength;
            var rampangle = sine * Mathf.Rad2Deg;
            var ramp = MeshE.HexahedronDraft(Settings.portSizeW, ramplength, height);
            ramp.Rotate(Quaternion.Euler(rampangle, 0, 0));
            
            //In order to be able to move this ramp reliably from outside this method, we place the ramp's start at its 'origin'.
            //That means moving it forward by 1/2 of its base width after rotating.
            var trueHeight = sine * ramplength;
            var trueWidth = Mathf.Sqrt((Mathf.Pow(ramplength, 2) - Mathf.Pow(trueHeight, 2)));
            ramp.Move(Vector3.forward * (trueWidth / 2)); //After this, the ramp is centered in the acces port.
            switch (dir)
            {
                //Clockwise?
                //Each direction has a small movement tweak to account for the effect of rotation.
                case Direction.West:
                    ramp.Move(Vector3.back * (Settings.portSizeL / 2));
                    break;
                case Direction.North:
                    ramp.Rotate(Quaternion.Euler(0, 90, 0));
                    ramp.Move(Vector3.left * (Settings.portSizeL / 2));
                    break;
                case Direction.East:
                    ramp.Rotate(Quaternion.Euler(0, 180, 0));
                    ramp.Move(Vector3.forward * (Settings.portSizeL / 2));
                    break;
                case Direction.South:
                    ramp.Rotate(Quaternion.Euler(0, 270, 0));
                    ramp.Move(Vector3.right * (Settings.portSizeL / 2));
                    break;
                default:
                    break;
            }
            ramp.Move(center + Vector3.up * height / 2);
            ramp.Move(Vector3.down * (Settings.baseFloorHeight / 2));
            return ramp;
        }
        //The outside corners of the floor
        public static Vector3[] getPillarCenters(Vector3 dimensions, float pillarWidth)
        {
            var right = Vector3.right * (dimensions.x - pillarWidth) / 2;
            var forward = Vector3.forward * (dimensions.z - pillarWidth) / 2;
            return new Vector3[]
                {
                -right - forward,
                right - forward,
                right + forward,
                -right + forward
                };
        }
        //The outside corners of the floor
        public static Vector3[] getPillarCenters(Vector3 dimensions, float pillarWidth, float currentFloor)
        {
            var right = Vector3.right * (dimensions.x - pillarWidth) / 2;
            var forward = Vector3.forward * (dimensions.z - pillarWidth) / 2;
            return new Vector3[]
                {
                -right - forward + (Vector3.up * (currentFloor * Settings.baseFloorHeight)),
                right - forward + (Vector3.up * (currentFloor * Settings.baseFloorHeight)),
                right + forward + (Vector3.up * (currentFloor * Settings.baseFloorHeight)),
                -right + forward + (Vector3.up * (currentFloor * Settings.baseFloorHeight))
                };
        }
        //The outside corners of the floor
        public static Vector3[] getPillarCenters(Vector3 center, Vector3 dimensions, float pillarWidth)
        {
            var right = Vector3.right * (dimensions.x - pillarWidth) / 2;
            var forward = Vector3.forward * (dimensions.z - pillarWidth) / 2;
            return new Vector3[]
                {
                center -right - forward,
                center + right - forward,
                center + right + forward,
                center -right + forward
                };
        }
        //The center of each side of the floor, filtered by direction
        public static Vector3 getFacadeCenters(Vector3 dimensions, float pillarWidth, Direction dir, float currentFloor)
        {
            var right = Vector3.right * (dimensions.x / 2 - (pillarWidth / 2));
            var forward = Vector3.forward * (dimensions.z / 2 - (pillarWidth / 2));
            switch (dir)
            {
                case Direction.East:
                    return -forward + (Vector3.up * (currentFloor * Settings.baseFloorHeight));
                case Direction.West:
                    return forward + (Vector3.up * (currentFloor * Settings.baseFloorHeight));
                case Direction.North:
                    return -right + (Vector3.up * (currentFloor * Settings.baseFloorHeight));
                case Direction.South:
                    return right + (Vector3.up * (currentFloor * Settings.baseFloorHeight));
                default:
                    return forward + (Vector3.up * (currentFloor * Settings.baseFloorHeight));
            }
        }
        /// <summary>
        /// Outside edge center of each side of the floor. Useful for weaving interfloor features in.
        /// </summary>
        /// <param name="dimensions"></param>
        /// <param name="pillarWidth"></param>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static Vector3[] getSideCenters(Vector3 dimensions, float pillarWidth, Direction dir, float currentFloor)
        {
            List<Vector3> centers = new List<Vector3>();
            var right = Vector3.right * (dimensions.x / 2);
            var forward = Vector3.forward * (dimensions.z / 2);
            centers.Add(-forward + (Vector3.up * (currentFloor * Settings.baseFloorHeight)));
            centers.Add(forward + (Vector3.up * (currentFloor * Settings.baseFloorHeight)));
            centers.Add(-right + (Vector3.up * (currentFloor * Settings.baseFloorHeight)));
            centers.Add(right + (Vector3.up * (currentFloor * Settings.baseFloorHeight)));
            return centers.ToArray();
        }
        public static Vector3[] getInnerPillarCentersUnitary(Vector3 dimensions, float pillarWidth, float modifier, float currentFloor)
        {
            //var modifier = UnityEngine.Random.Range(0.6f, exclusion[0]);
            var midright = (Vector3.right * (dimensions.x - pillarWidth - modifier) / 2);
            var midforward = (Vector3.forward * (dimensions.z - pillarWidth - modifier) / 2);
            return new Vector3[]
                {
                -midright - midforward + (Vector3.up * (currentFloor * Settings.baseFloorHeight)),
                midright - midforward + (Vector3.up * (currentFloor * Settings.baseFloorHeight)),
                midright + midforward + (Vector3.up * (currentFloor * Settings.baseFloorHeight)),
                -midright + midforward + (Vector3.up * (currentFloor * Settings.baseFloorHeight))
                };
        }
        public static Vector3[] getInnerPillarCentersUnitary(Vector3 dimensions, float pillarWidth, float modifier, Direction prevdir, Direction dir, float currentFloor)
        {
            Dictionary<Direction, Vector3> points = new Dictionary<Direction, Vector3>();
            List<Vector3> final = new List<Vector3>();
            var midright = (Vector3.right * (dimensions.x - pillarWidth - modifier) / 2);
            var midforward = (Vector3.forward * (dimensions.z - pillarWidth - modifier) / 2);
            points.Add(Direction.West, -midright - midforward + (Vector3.up * (currentFloor * Settings.baseFloorHeight)));
            points.Add(Direction.North, midright - midforward + (Vector3.up * (currentFloor * Settings.baseFloorHeight)));
            points.Add(Direction.East, midright + midforward + (Vector3.up * (currentFloor * Settings.baseFloorHeight)));
            points.Add(Direction.South, -midright + midforward + (Vector3.up * (currentFloor * Settings.baseFloorHeight)));
            //Some misleading stuff here.
            //previous floor is below the current floor. features of current floor protrude down- so if the previous floor
            //has an access ramp aperture, we don't want pillars sticking into it.
            switch (prevdir)
            {
                case Direction.East:
                    points.Remove(Direction.East);
                    break;
                case Direction.West:
                    points.Remove(Direction.West);
                    break;
                case Direction.North:
                    points.Remove(Direction.West);
                    break;
                case Direction.South:
                    points.Remove(Direction.East);
                    break;
                default:
                    break;
            }
            //We don't want pillars to coincide with the current floor's access port either.
            //The ramps that run north and south are on the 'short' side of the floor rectangle and so can interfere with two ramps instead of one.
            switch (dir)
            {
                case Direction.East:
                    points.Remove(Direction.East);
                    break;
                case Direction.West:
                    points.Remove(Direction.West);
                    break;
                case Direction.North:
                    points.Remove(Direction.North);
                    points.Remove(Direction.West);
                    break;
                case Direction.South:
                    points.Remove(Direction.East);
                    points.Remove(Direction.South);
                    break;
                default:
                    break;
            }
            foreach (KeyValuePair<Direction, Vector3> pair in points)
            {
                final.Add(pair.Value);
            }
            return final.ToArray();
        }
        /// <summary>
        /// Compromise when floors change sizes so pillars don't hang in space above partial floors.
        /// </summary>
        /// <param name="oldSize"></param>
        /// <param name="newSize"></param>
        /// <returns></returns>
        public static Vector3 compromiseSize(Vector3 oldSize, Vector3 newSize)
        {
            Vector3 compromise = newSize;
            if (oldSize.x < newSize.x) compromise.x = oldSize.x;
            if (oldSize.z < newSize.z) compromise.z = oldSize.z;
            return compromise;
        }
        public static int[] midpoint(int[] pointA, int[] pointB)
        {
            var mp = new int[2];
            mp[0] = (pointA[0] + pointB[0]) / 2;
            mp[1] = (pointA[1] + pointB[1]) / 2;
            return mp;
        }
        public static Direction dirFrom2dCoords(int[] a, int[] b)
        {
            var dir = Direction.North;
            int[] comp = { (a[0] - b[0]), (a[1] - b[1]) };
            if (comp[0] == 0)//if the x dimension difference is 0, it can only be north or south
            {
                if (comp[1] > 0) dir = Direction.North;
                if (comp[1] < 0) dir = Direction.South;
            }
            else
            {
                if (comp[0] > 0) dir = Direction.East;
                if (comp[0] < 0) dir = Direction.West;
            }
            return dir;
        }
        /// <summary>
        /// Out of two equal groups of points, find the two that are closest together.
        /// </summary>
        /// <param name="wpA"></param>
        /// <param name="wpB"></param>
        public static Vector3[] compromisePoints(Vector3[] wpA, Vector3[] wpB)
        {
            var arr = new Vector3[2];
            List<wayPointComparator> distPairs = new List<wayPointComparator>();
            var key = 0;
            for (int i = 0; i < wpA.Length; i++)
            {
                for (int j = 0; j < wpB.Length; j++)
                {
                    var dist = Vector3.Distance(wpA[i], wpB[j]);
                    var wpc = new wayPointComparator(dist, i, j);
                    distPairs.Add(wpc);
                    key++;
                }
            }
            var items = distPairs.OrderBy(i => i.distance).ToList();
            arr[0] = wpA[items[0].itemi];
            arr[1] = wpB[items[0].itemj];
            return arr;
        }
        public static MeshDraft Pillar0(Vector3 center, float width, float height, float elevation)
        {
            var draft = MeshE.HexahedronDraft(width, width, height);
            draft.Move(center + Vector3.down * (height / 2));
            return draft;
        }
        public static MeshDraft Facade(Vector3 center, float thickness, float length, float width, float height, float elevation, Direction dir)
        {
            //var rotation = 0.0f;
            var myLength = 0.0f;
            var myThickness = 0.0f;
            var modifier = 1.0f;
            if (RandomE.Chance(0.5f))
            {
                modifier = 0.5f;
            }
            if (dir == Direction.East | dir == Direction.West)
            {
                myLength = thickness;
                myThickness = (width - (thickness * 2)) * modifier; //long edge of facade is 2 pillars shorter than the floor it's on.
            }
            else
            {
                myThickness = thickness;
                myLength = (length - (thickness * 2)) * modifier;
            }
            var draft = MeshE.HexahedronDraft(myThickness, myLength, (height - Settings.baseFloorSize.y));
            draft.Move(center + Vector3.down * ((height - Settings.baseFloorSize.y) / 2));
            return draft;
        }

        public static MeshDraft Floor0(Vector3 center, float width, float length, float height)
        {
            var draft = MeshE.HexahedronDraft(width, length, height);
            draft.Move(center + Vector3.up * height / 2);
            return draft;
        }
        public static MeshDraft[] PillarSet(Vector3[] centers, float height, float elevation, float density, float pillarWidth)
        {
            var _pillars = new List<MeshDraft>();
            foreach (Vector3 center in centers)
            {
                if (RandomE.Chance(density)) { _pillars.Add(Pillar0(center, pillarWidth, (height - Settings.baseFloorSize.y), elevation)); }
            }
            return _pillars.ToArray();
        }
    }
}
