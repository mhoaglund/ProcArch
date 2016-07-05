using System;
using UnityEngine;
using System.Collections;
using ProceduralToolkit;
using System.Collections.Generic;
using Assets.Core;

namespace Assets.Core
{
    static class Settings
    {
        //Structure settings
        public static readonly Vector3 baseFloorSize = new Vector3(10.0f, 0.15f, 15.0f);
        public static readonly float baseFloorHeight = 3.0f;
        public static readonly float MaxFloors = 24.0f;

        //Grotto settings
        public static int grottoResolution = 2;

        //Overall field settings
        public static readonly float margin = 25.0f;
        public static readonly int densityLB = 1;
        public static readonly int densityUB = 3;
        public static readonly int rowSize = 5;
        public static readonly int mapSizeX = 100;
        public static readonly int mapSizeZ = 200;

        //Interstructure Feature Settings
        public static readonly float cableDensityLB = 0.05f;
        public static readonly float cableDensityUB = 0.3f;
        public static readonly float fireEscapeWidth = 2.5f;

        public static readonly float portSizeL = 2.0f;
        public static readonly float portSizeW = 1.0f;
        public static readonly float portOffsetX = 3.0f;
        public static readonly float portOffsetZ = 2.0f;

        public static readonly float partialUnit = 2.0f;
    }
}
