using System;
using UnityEngine;
using System.Collections;
using ProceduralToolkit;
using System.Collections.Generic;
using Assets.Core;

namespace Assets.Core
{
    class CatwalkGenerator
    {
        public Vector3 startpoint;
        public Vector3 endpoint;
        public int floorsHigh;
        public MasterInfrastructure.Orientation orientation;
        public MeshDraft _catwalk = new MeshDraft();

        public CatwalkGenerator(Vector3 start, Vector3 end, int floors, MasterInfrastructure.Orientation _orientation)
        {
            startpoint = start;
            endpoint = end;
            floorsHigh = floors;
            orientation = _orientation;
            _catwalk = Catwalk();
        }

        public MeshDraft Catwalk()
        {
            MeshDraft cw = new MeshDraft();
            var floormodifier = (Settings.baseFloorHeight) * floorsHigh;
            var length = Vector3.Distance(startpoint, endpoint);
            var center = (startpoint / 2) + (endpoint / 2);
            if(orientation == MasterInfrastructure.Orientation.X){
                cw = MeshE.HexahedronDraft(2.0f, length, Settings.baseFloorSize.y);
            }
            else cw = MeshE.HexahedronDraft(length, 2.0f, Settings.baseFloorSize.y);

            cw.Move(center + Vector3.up * ((Settings.baseFloorSize.y/2)));
            return cw;
        }
    }
}
