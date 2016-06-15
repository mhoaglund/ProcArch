using System;
using UnityEngine;
using System.Collections;
using ProceduralToolkit;
using System.Collections.Generic;
using Assets.Core;

namespace Assets.Core
{
    /// <summary>
    /// A procedural library generator object.
    /// </summary>
    public class LibraryGenerator
    {

        public List<FloorInfo> floorManifest = new List<FloorInfo>();
        public List<List<int>> gapSets = new List<List<int>>();
        public List<int> gapFloor = new List<int>();
        public int[] blockCoordinates = new int[2];
        public int id;
        private float atriumLB = 0.65f;
        private float atriumUB = 0.80f;
        private float atrium;
        private float pillarModifier = 3.0f;

        private Vector3 floorDimensions = Settings.baseFloorSize;
        private float floorHeight = Settings.baseFloorHeight;

        private float pillarwidthLB = 0.08f;
        private float pillarwidthUB = 0.7f;
        private float pillarWidth;

        public Vector3 lightpoint = new Vector3();

        public MeshDraft Building;
        public MeshDraft Windows;
        public Vector3[] wayPoint = new Vector3[4];

        private GameObject myHost;

        public LibraryGenerator(GameObject host)
        {
            myHost = host;
            Building = Library();
        }

        //TODO: create "fire escape" routine with a series of boxes and ramps on the side of the floors.

        private MeshDraft Library()
        {
            floorManifest.Clear();
            pillarWidth = UnityEngine.Random.Range(pillarwidthLB, pillarwidthUB);
            atrium = UnityEngine.Random.Range(atriumLB, atriumUB);

            var maxFloors = UnityEngine.Random.Range(3.0f, Settings.MaxFloors);
            var floors = (int)Math.Ceiling(UnityEngine.Random.Range(1.0f, maxFloors));
            var mylightpoint = Vector3.up * ((Settings.baseFloorHeight * UnityEngine.Random.Range(floors/2, floors)) - (Settings.baseFloorHeight/2));
            lightpoint = myHost.transform.TransformPoint(mylightpoint);

            var myId = Guid.NewGuid();
            var _library = new MeshDraft { name = myId.ToString() };
            for (int i = 0; i < floors; i++)
            {
                foreach (MeshDraft mesh in FloorWithPillars(i, 0.9f))
                {
                    _library.Add(mesh);
                }
            }

            CleanGapFloors();
            return _library;
        }

        //the gapfloors list should document only the contiguous non-traversable areas of a structure
        private void CleanGapFloors()
        {
            //Lots of work to do something simple here. From a list of numbers, grab only those that fall in series with one another.
            //If multiple series are found, store them in separate lists so we can iterate independently later to make features on floors.
            if (gapFloor.Count < 2)
            {
                if(floorManifest.Count> 5)
                {
                    List<int> newGap = new List<int>();
                    var simulatedGap = UnityEngine.Random.Range(floorManifest.Count / 2, floorManifest.Count);
                    newGap.Add(simulatedGap - 2);
                    newGap.Add(simulatedGap - 1);
                    newGap.Add(simulatedGap);
                    gapSets.Add(newGap);
                }
            }
            else
            {
                List<int> newGap = new List<int>();
                for (int i = 0; i < gapFloor.Count; i++)
                {
                    int prevdiff = 0;
                    int nextdiff = 0;
                    if (i == 0) prevdiff = 0;
                    else prevdiff = Math.Abs(gapFloor[i] - gapFloor[i - 1]);

                    if (i == (gapFloor.Count - 1)) nextdiff = 0;
                    else nextdiff = Math.Abs(gapFloor[i] - gapFloor[i + 1]);

                    if (prevdiff != 1)
                    {
                        newGap.Add(-1);
                        if (newGap.Count > 3)
                        {
                            newGap.RemoveAll(item => item == -1);
                            if (newGap.Count > 1)
                            {
                                var tempstore = new List<int>(newGap);
                                gapSets.Add(tempstore);
                                newGap.Clear();
                            }
                        }
                    }
                    if (nextdiff == 1 | prevdiff == 1)
                    {
                        newGap.Add(gapFloor[i] - 1);
                    }
                    if (nextdiff != 1)
                    {
                        newGap.Add(-1);
                        if (newGap.Count > 3)
                        {
                            newGap.RemoveAll(item => item == -1);
                            if (newGap.Count > 1)
                            {
                                var tempstore = new List<int>(newGap);
                                gapSets.Add(tempstore);
                                newGap.Clear();
                            }
                        }
                    }
                }
                //Add an additional floor to the end of the gapsets.
                foreach (List<int> gap in gapSets)
                {
                    var lastitem = gap[gap.Count - 1];
                    if (lastitem < floorManifest.Count & floorManifest.Count > 6)
                    {
                        gap.Add(lastitem + 1);
                    }
                }
            }
        }

        private void UpdateManifest(FloorType arg, bool trav, Vector3 dims, Direction dir, Vector3[] sideCenters, Vector3[] pillarCenters)
        {
            sideCenters[0] = myHost.transform.TransformPoint(sideCenters[0]);
            sideCenters[1] = myHost.transform.TransformPoint(sideCenters[1]);
            sideCenters[2] = myHost.transform.TransformPoint(sideCenters[2]);
            sideCenters[3] = myHost.transform.TransformPoint(sideCenters[3]);

            pillarCenters[0] = myHost.transform.TransformPoint(pillarCenters[0]);
            pillarCenters[1] = myHost.transform.TransformPoint(pillarCenters[1]);
            pillarCenters[2] = myHost.transform.TransformPoint(pillarCenters[2]);
            pillarCenters[3] = myHost.transform.TransformPoint(pillarCenters[3]);

            FloorInfo fi = new FloorInfo(arg, trav, dims, dir, sideCenters, pillarCenters);
            floorManifest.Add(fi);
            if(trav == false && floorManifest.Count > 4)
            {
                gapFloor.Add(floorManifest.Count);
            }
        }

        //Lots of complex logic here. Could simplify this by making a method that returns a set of floorinfo's that we can loop over and turn into our mesh.
        private MeshDraft[] FloorWithPillars(int currentFloor, float pillarDensity)
        {
            var _floor = new List<MeshDraft>();
            bool useInners = true;
            bool traversability = false;

            Direction myDir = Direction.North;
            if (RandomE.Chance(0.25f)) myDir = Direction.West;
            else if (RandomE.Chance(0.25f)) myDir = Direction.East;
            else if (RandomE.Chance(0.50f)) myDir = Direction.South;
            else if (RandomE.Chance(0.25f)) myDir = Direction.North;
            else myDir = Direction.West;


            //Getting the floorinfo for the previous floor
            FloorInfo _prevFloor = (currentFloor < 1) ? null : floorManifest[(currentFloor - 1)];
            if (_prevFloor != null) useInners = (_prevFloor.fType != FloorType.Atrium & _prevFloor.fType != FloorType.PartialAtrium) ? true : false;

            //Tracking the floor type here for when we have to place staircases and keep multistory features in check.
            FloorType currentType = FloorType.Full;

            var floorDims = floorDimensions;

            //30% of the time, we have a partial floor that is reduced in size in x or z.
            if (RandomE.Chance(0.3f))
            {
                floorDims = (RandomE.Chance(0.4f)) ? new Vector3(floorDims.x, floorDims.y, (floorDims.z - Settings.partialUnit)) : new Vector3((floorDims.x - Settings.partialUnit), floorDims.y, floorDims.z);
                currentType = FloorType.Partial;
            }

            if (currentFloor == 0)
            {
                _floor.Add(BldgUtils.Floor0(Vector3.up * (currentFloor * floorHeight), floorDims.x, floorDims.z, floorDims.y));
                foreach (MeshDraft pillar in BldgUtils.PillarSet(BldgUtils.getInnerPillarCentersUnitary(floorDims, pillarWidth, pillarModifier, currentFloor), floorHeight, floorHeight * currentFloor, pillarDensity, pillarWidth))
                {
                    _floor.Add(pillar);
                }
                traversability = true;
            }
            else
            {
                if (RandomE.Chance(0.4f))
                {
                    foreach (MeshDraft mesh in BldgUtils.AtriumFloor(Vector3.up * (currentFloor * floorHeight), floorDims.x, floorDims.z, floorDims.y, atrium, useInners))
                    {
                        _floor.Add(mesh);
                    }
                    traversability = useInners;
                    if (currentType == FloorType.Partial) currentType = FloorType.PartialAtrium;
                    else currentType = FloorType.Atrium;
                }
                else
                {
                    if (useInners)
                    {
                        foreach (MeshDraft mesh in BldgUtils.AccessFloorUnitary(Vector3.up * (currentFloor * floorHeight), floorDims, myDir))
                        {
                            _floor.Add(mesh);
                            traversability = true;
                        }
                        if (currentType == FloorType.Partial) currentType = FloorType.PartialAccess;
                        else currentType = FloorType.Access;
                    }
                    else _floor.Add(BldgUtils.Floor0(Vector3.up * (currentFloor * floorHeight), floorDims.x, floorDims.z, floorDims.y));
                }
            }

            //determining outer side centers before modifying floordims. should refactor this so its not so state-dependent.
            var sidecenters = BldgUtils.getSideCenters(floorDims, pillarWidth, myDir, currentFloor);
            var _pillarCenters = BldgUtils.getPillarCenters(floorDims, pillarWidth, currentFloor);
            //Pillar Setup. We use a slightly different set of dimensions to place this to maintain compatibility between differently-sized floors.
            var pillarDims = floorDims;
            if (currentFloor > 0)
            {
                
                if (_prevFloor.fType == FloorType.Partial | 
                    _prevFloor.fType == FloorType.PartialAtrium | 
                    _prevFloor.fType == FloorType.PartialAccess)
                    pillarDims = BldgUtils.compromiseSize(_prevFloor.dimensions, floorDims);
                _pillarCenters = BldgUtils.getPillarCenters(pillarDims, pillarWidth, currentFloor); //kludgy redo here
                foreach (MeshDraft pillar in BldgUtils.PillarSet(_pillarCenters, floorHeight, floorHeight * currentFloor, pillarDensity, pillarWidth))
                {
                    _floor.Add(pillar);
                }

                //need to set these per floor
                wayPoint[0] = _pillarCenters[0];
                wayPoint[1] = _pillarCenters[1];
                wayPoint[2] = _pillarCenters[2];
                wayPoint[3] = _pillarCenters[3];

                //60 % chance of inner pillars, but if the previous floor was an atrium we don't want them.
                if (RandomE.Chance(0.6f))
                {
                    if (_prevFloor.fType != FloorType.Atrium & _prevFloor.fType != FloorType.PartialAtrium)
                    {
                        foreach (MeshDraft pillar in BldgUtils.PillarSet(BldgUtils.getInnerPillarCentersUnitary(pillarDims, pillarWidth, pillarModifier, _prevFloor.direction, myDir, currentFloor), floorHeight, floorHeight * currentFloor, pillarDensity, pillarWidth))
                        {
                            _floor.Add(pillar);
                        }
                    }
                }
            }

            //Facade handling. We do this after the pillars because we want the compromised floordims.
            if (RandomE.Chance(0.7f))
            {
                _floor.Add(
                    BldgUtils.Facade(
                        BldgUtils.getFacadeCenters(pillarDims, pillarWidth, myDir, currentFloor), 
                        pillarWidth, pillarDims.z, pillarDims.x, floorHeight, floorHeight * currentFloor, myDir)
                    );
                if (RandomE.Chance(0.6f)) _floor.Add(BldgUtils.Facade(BldgUtils.getFacadeCenters(pillarDims, pillarWidth, myDir.Next(), currentFloor), pillarWidth, pillarDims.z, pillarDims.x, floorHeight, floorHeight * currentFloor, myDir.Next()));
                else {
                    if (Windows == null) Windows = new MeshDraft();
                    Windows.Add(BldgUtils.Facade(
                     BldgUtils.getFacadeCenters(pillarDims, pillarWidth, myDir.Next(), currentFloor),
                     pillarWidth, pillarDims.z, pillarDims.x, floorHeight, floorHeight * currentFloor, myDir.Next())
                 );
                } 
            }

            UpdateManifest(currentType, traversability, floorDims, myDir, sidecenters, _pillarCenters);
            return _floor.ToArray();
        }

        private MeshDraft[] FloorWithOrnaments(int currentFloor, Vector3[] pillarCenters, Vector3[] innerPillarCenters, float pillarDensity, float pillarWidth)
        {
            //TODO: what is the intent here?
            var _floor = new List<MeshDraft>();
            return _floor.ToArray();
        }
    }

}
