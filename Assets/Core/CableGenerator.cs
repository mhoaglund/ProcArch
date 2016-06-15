using System;
using UnityEngine;
using System.Collections;
using ProceduralToolkit;
using System.Collections.Generic;

namespace Assets.Core
{
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    class CableGenerator
    {
        public Material[] _materials = new Material[1];
        public MeshDraft _cable = new MeshDraft();
        public string _angleName = "0";

        public CableGenerator(Vector3[] points, float slack, int res, float diameter)
        {
            _cable.Add(Cable(points, slack, res, diameter));
        }

        public CableGenerator(Vector3[] points, float slack, int res, float diameter, GameObject go)
        {
            _cable.Add(Cable(points, slack, res, diameter));
        }

        private MeshDraft Cable(Vector3[] endpoints, float slack, int resolution, float diameter)
        {
            var start = endpoints[0];
            var end = endpoints[1];
            var lookdirection = start - end;
            Quaternion rotation = Quaternion.LookRotation(lookdirection);
            _angleName = rotation.eulerAngles.y.ToString();
            Vector3[] startCorners = LoftCorners(start, diameter, rotation);
            Vector3[] prevCorners = (Vector3[])startCorners.Clone();
            var xdiff = end.x - start.x;
            var ydiff = end.y - start.y;
            var zdiff = end.z - start.z;
            List<float> pointslist = new List<float>();
            var fres = 9.5f / resolution;
            for(int i = 0; i <= resolution; i++)
            {
                float cfloat = 1.0f + (fres) * i;
                pointslist.Add(cfloat);
            }
            float[] points = pointslist.ToArray();

            List<float> droops = new List<float>();
            var xstep = xdiff / points.Length;
            var zstep = zdiff / points.Length;
            var ystep = ydiff / points.Length;
            var draft = new MeshDraft();

            for (int i = 0; i < points.Length; i++)
            {
                //Lots of funny stuff surrounding the droop effect here. We want to incrementally droop the cable according to a parabola,
                //but we only want the 'rate of change' of the parabola not the actual y output of the equation.
                var droop = (Mathf.Pow((points[i]), 2) - ((11) * points[i]) + 10.0f) * slack;
                var finaldroop = 0.0f;
                var initialdroop = 1.0f - fres;
                if (i == 0)
                {
                    finaldroop = droop - ((Mathf.Pow((initialdroop), 2) - ((11) * initialdroop) + 10.0f) * slack);
                }
                else
                {
                    var prevdroop = (Mathf.Pow((points[i - 1]), 2) - ((11) * points[i - 1]) + 10.0f) * slack;
                    droop = (Mathf.Pow((points[i]), 2) - ((11) * points[i]) + 10.0f) * slack;
                    finaldroop = droop - prevdroop;
                }
                
                droops.Add(droop);

                Vector3[] ptcorners =
                {
                    new Vector3((prevCorners[0].x + xstep), prevCorners[0].y + ystep + finaldroop, prevCorners[0].z + zstep),
                    new Vector3((prevCorners[1].x + xstep), prevCorners[1].y + ystep + finaldroop, prevCorners[1].z + zstep),
                    new Vector3((prevCorners[2].x + xstep), prevCorners[2].y + ystep + finaldroop, prevCorners[2].z + zstep),
                    new Vector3((prevCorners[3].x + xstep), prevCorners[3].y + ystep + finaldroop, prevCorners[3].z + zstep)
                };

                draft.Add(MeshE.QuadDraft(ptcorners[0], ptcorners[1], prevCorners[1], prevCorners[0]));
                draft.Add(MeshE.QuadDraft(ptcorners[1], ptcorners[2], prevCorners[2], prevCorners[1]));
                draft.Add(MeshE.QuadDraft(ptcorners[2], ptcorners[3], prevCorners[3], prevCorners[2]));
                draft.Add(MeshE.QuadDraft(ptcorners[3], ptcorners[0], prevCorners[0], prevCorners[3]));

                prevCorners[0] = ptcorners[0];
                prevCorners[1] = ptcorners[1];
                prevCorners[2] = ptcorners[2];
                prevCorners[3] = ptcorners[3];
            }
            return draft;
        }

        private Vector3[] LoftCorners(Vector3 origin, float diameter, Quaternion direction)
        {
            List<Vector3> startCorners = new List<Vector3>();
            var halfdiameter = diameter / 2;
            var eulerdir = direction.eulerAngles.y;
            //This is a little wonky, but we have to rotate this set of starting points to match the heading of the cable.
            //Since we build cable meshes in world space, we can't effectively turn this starting square of points into a meshdraft and rotate it normally.
            //Not sure if this is worth refactoring, but there are def. more elegant solutions.
            if (eulerdir < 90 | eulerdir > 340)
            {
                startCorners.Add(new Vector3(origin.x - halfdiameter, origin.y, origin.z));
                startCorners.Add(new Vector3(origin.x - halfdiameter, origin.y - diameter, origin.z));
                startCorners.Add(new Vector3(origin.x + halfdiameter, origin.y - diameter, origin.z));
                startCorners.Add(new Vector3(origin.x + halfdiameter, origin.y, origin.z));
            }
            else if (eulerdir > 90 & eulerdir < 150)
            {
                startCorners.Add(new Vector3(origin.x, origin.y, origin.z + halfdiameter));
                startCorners.Add(new Vector3(origin.x, origin.y - diameter, origin.z + halfdiameter));
                startCorners.Add(new Vector3(origin.x, origin.y - diameter, origin.z - halfdiameter));
                startCorners.Add(new Vector3(origin.x, origin.y, origin.z - halfdiameter));
            }
            else if (eulerdir > 150 & eulerdir < 270)
            {
                startCorners.Add(new Vector3(origin.x + halfdiameter, origin.y, origin.z));
                startCorners.Add(new Vector3(origin.x + halfdiameter, origin.y - diameter, origin.z));
                startCorners.Add(new Vector3(origin.x - halfdiameter, origin.y - diameter, origin.z));
                startCorners.Add(new Vector3(origin.x - halfdiameter, origin.y, origin.z));
            }
            else if (eulerdir > 270 & eulerdir < 340)
            {
                startCorners.Add(new Vector3(origin.x, origin.y, origin.z - halfdiameter));
                startCorners.Add(new Vector3(origin.x, origin.y - diameter, origin.z - halfdiameter));
                startCorners.Add(new Vector3(origin.x, origin.y - diameter, origin.z + halfdiameter));
                startCorners.Add(new Vector3(origin.x, origin.y, origin.z + halfdiameter));
            }

            if (startCorners.Count == 0) return startCorners.ToArray();
            var startSquare = new MeshDraft();
            //since our cablegenerator is as 0,0,0 in space, we can't rely on the base class to get the position of vertices right when we init this quad.
            //hence the ridiculous hardcoding of the vertices before rotation.
            startSquare.Add(MeshE.QuadDraft(startCorners[0], startCorners[1], startCorners[2], startCorners[3]));
            startSquare.vertices = startCorners;
            //startSquare.Rotate(direction);
            //startSquare.Rotate(Quaternion.Euler(0,90,0));
            return startSquare.vertices.ToArray();
            
            //return startCorners;
        }
    }
}
