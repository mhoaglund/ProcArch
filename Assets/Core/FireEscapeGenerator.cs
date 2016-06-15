using System;
using UnityEngine;
using System.Collections;
using ProceduralToolkit;
using System.Collections.Generic;
using Assets.Core;

namespace Assets.Core
{
    class FireEscapeGenerator
    {
        public List<FloorInfo> floorManifest = new List<FloorInfo>();
        public List<int> escapedFloors;
        public Direction direction;
        public MeshDraft _fireEscape = new MeshDraft();

        public FireEscapeGenerator(List<FloorInfo> allFloors, List<int> _escapedFloors, Direction dir)
        {
            floorManifest = allFloors;
            escapedFloors = _escapedFloors;
            direction = dir;

            foreach (MeshDraft mesh in FireEscape())
            {
                _fireEscape.Add(mesh);
            }
        }

        public MeshDraft[] FireEscape()
        {
            var _floor = new List<MeshDraft>();
            var startPoint = new Vector3();
            var width = 0.0f;
            var length = 0.0f;
            var movedir = Vector3.forward;
            bool firstLanding = true;
            var pillarWidth = 0.1f;

            foreach (int floor in escapedFloors)
            {
                var partialmod = 0.0f;
                //Problem: partial floors can vary in size in two directions. This case handles some cases, but not all.
                //A better way would be to cue off of the actual dimensions of the floor itself. Floorinfos have that built in.
                //if (floorManifest[floor - 1].fType == FloorType.Partial | floorManifest[floor - 1].fType == FloorType.PartialAccess | floorManifest[floor - 1].fType == FloorType.PartialAtrium)
                //{
                //    partialmod = Settings.partialUnit / 2;
                //}
                //var currfloor = (floor < (floorManifest.Count-1)) ? floor + 1 : floorManifest.Count-1;
                var trueLandingWidthX = Settings.fireEscapeWidth + ((Settings.baseFloorSize.x - floorManifest[floor-1].dimensions.x)/2);
                var trueLandingWidthZ = Settings.fireEscapeWidth + ((Settings.baseFloorSize.z - floorManifest[floor-1].dimensions.z)/2);
                var trueLandingWidth = 0.0f;
                switch (direction)
                {
                    //These directions check out okay.
                    case Direction.North:
                        startPoint = floorManifest[floor-1].sideCenters[0];
                        length = Settings.baseFloorSize.x - Settings.partialUnit;
                        width = trueLandingWidthZ;
                        trueLandingWidth = trueLandingWidthZ;
                        movedir = Vector3.back;
                        break;
                    case Direction.South:
                        startPoint = floorManifest[floor-1].sideCenters[1];
                        length = Settings.baseFloorSize.x - Settings.partialUnit;
                        width = trueLandingWidthZ;
                        trueLandingWidth = trueLandingWidthZ;
                        movedir = Vector3.forward;
                        break;
                    case Direction.East:
                        startPoint = floorManifest[floor-1].sideCenters[2];
                        length = trueLandingWidthX;
                        trueLandingWidth = trueLandingWidthX;
                        width = Settings.baseFloorSize.z - Settings.partialUnit;
                        movedir = Vector3.left;
                        break;
                    case Direction.West:
                        startPoint = floorManifest[floor-1].sideCenters[3];
                        length = trueLandingWidthX;
                        trueLandingWidth = trueLandingWidthX;
                        width = Settings.baseFloorSize.z - Settings.partialUnit;
                        movedir = Vector3.right;
                        break;
                    default:
                        break;
                }

                var fldims = new Vector3(length, Settings.baseFloorSize.y, width);
                if (firstLanding)
                {
                    var landing = BldgUtils.Floor0(startPoint, fldims.x, fldims.z, fldims.y);
                    landing.Move(movedir * (trueLandingWidth / 2));
                    _floor.Add(landing);
                }
                else
                {
                    var landing = BldgUtils.AccessFloorUnitary(startPoint, fldims, direction, 1.0f, 3.0f);
                    foreach (MeshDraft md in landing)
                    {
                        md.Move(movedir * (trueLandingWidth / 2));
                        _floor.Add(md);
                    }
                    var _pillarCenters = BldgUtils.getPillarCenters((startPoint + (movedir * (trueLandingWidth / 2))), fldims, pillarWidth);

                    foreach (MeshDraft pillar in BldgUtils.PillarSet(_pillarCenters, Settings.baseFloorHeight, Settings.baseFloorHeight * (floor), 1.0f, pillarWidth))
                    {
                        _floor.Add(pillar);
                    }
                }

                //landing.Move(movedir * (Settings.fireEscapeWidth /2));
                firstLanding = false;
            }

            return _floor.ToArray();
        }
    }
}
