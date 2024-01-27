using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Media;
using Microsoft.Kinect.Face;
using System.Windows;

namespace LumiosNoctis
{
    internal class FaceMesh : IDisposable
    {
        KinectManager kinect;
        MeshGeometry3D theGeometry;
        /// <summary>
        /// FaceModel is a result of capturing a face
        /// </summary>
        private FaceModel currentFaceModel = null;

        /// <summary>
        /// FaceModelBuilder is used to produce a FaceModel
        /// </summary>
        public FaceModelBuilder faceModelBuilder = null;

        /// <summary>
        /// FaceAlignment is the result of tracking a face, it has face animations location and orientation
        /// </summary>
        private FaceAlignment currentFaceAlignment = null;

        public void Dispose()
        {
            if (currentFaceModel != null)
            {
                currentFaceModel.Dispose();
                currentFaceModel = null;
            }
            if (faceModelBuilder != null)
            {
                faceModelBuilder.Dispose();
                faceModelBuilder = null;
            }
        }

        public FaceMesh(KinectManager kinectManager)
        {
            kinect = kinectManager;
            theGeometry = kinect.mainWindow.theGeometry;
            currentFaceModel = new FaceModel();
            currentFaceAlignment = new FaceAlignment();

            InitializeMesh();
            UpdateMesh();
        }

        /// <summary>
        /// Initializes a 3D mesh to deform every frame
        /// </summary>
        private void InitializeMesh()
        {
            var vertices = currentFaceModel.CalculateVerticesForAlignment(currentFaceAlignment);

            var triangleIndices = currentFaceModel.TriangleIndices;

            var indices = new Int32Collection(triangleIndices.Count);

            for (int i = 0; i < triangleIndices.Count; i += 3)
            {
                uint index01 = triangleIndices[i];
                uint index02 = triangleIndices[i + 1];
                uint index03 = triangleIndices[i + 2];

                indices.Add((int)index03);
                indices.Add((int)index02);
                indices.Add((int)index01);
            }
            theGeometry.TriangleIndices = indices;
            theGeometry.Normals = null;
            theGeometry.Positions = new Point3DCollection();
            theGeometry.TextureCoordinates = new PointCollection();

            foreach (var vert in vertices)
            {
                theGeometry.Positions.Add(new Point3D(vert.X, vert.Y, -vert.Z));
                theGeometry.TextureCoordinates.Add(new Point());
            }
        }

        private void UpdateFacialExpressions()
        {
            float jawOpenValue = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.JawOpen];
            var pivotPoint = currentFaceAlignment.HeadPivotPoint;
            Point3D upperLipPos = theGeometry.Positions[(int)HighDetailFacePoints.MouthUpperlipMidbottom];
            Point3D lowerLipPos = theGeometry.Positions[(int)HighDetailFacePoints.MouthLowerlipMidtop];
            //float rightSmirkOpenValue = currentFaceAlignment.AnimationUnits[FaceShapeAnimations.JawSlideRight]
            Console.WriteLine(jawOpenValue);
        }

        /// <summary>
        /// Sends the new deformed mesh to be drawn
        /// </summary>
        private void UpdateMesh()
        {
            var vertices = currentFaceModel.CalculateVerticesForAlignment(currentFaceAlignment);

            for (int i = 0; i < vertices.Count; i++)
            {
                var vert = vertices[i];
                theGeometry.Positions[i] = new Point3D(vert.X, vert.Y, -vert.Z);
            }
        }

        /// <summary>
        /// This event fires when the face capture operation is completed
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        public void HdFaceBuilder_CollectionCompleted(object sender, FaceModelBuilderCollectionCompletedEventArgs e)
        {
            var modelData = e.ModelData;

            this.currentFaceModel = modelData.ProduceFaceModel();

            this.faceModelBuilder.Dispose();
            this.faceModelBuilder = null;

           // return "Capture Complete";
        }


        public void StartCapture(in HighDefinitionFaceFrameSource highDefinitionFaceFrameSource)
        {
            this.faceModelBuilder = null;

            this.faceModelBuilder = highDefinitionFaceFrameSource.OpenModelBuilder(FaceModelBuilderAttributes.None);

            this.faceModelBuilder.BeginFaceDataCollection();

            this.faceModelBuilder.CollectionCompleted += this.HdFaceBuilder_CollectionCompleted;
        }

        public void StopCapture()
        {
            if (this.faceModelBuilder != null)
            {
                this.faceModelBuilder.Dispose();
                this.faceModelBuilder = null;
            }
        }

        public void FrameArrived(in HighDefinitionFaceFrame frame)
        {
            frame.GetAndRefreshFaceAlignmentResult(currentFaceAlignment);

            this.UpdateMesh();
            this.UpdateFacialExpressions();
        }
    }
}
