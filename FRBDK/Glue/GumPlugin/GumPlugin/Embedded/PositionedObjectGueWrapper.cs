﻿using FlatRedBall;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace GumCoreShared.FlatRedBall.Embedded
{
    public class PositionedObjectGueWrapper : PositionedObject
    {
        PositionedObject frbObject;
        global::FlatRedBall.Math.Geometry.IReadOnlyScalable frbObjectAsScalable;

        public PositionedObject FrbObject
        {
            get { return frbObject; }
            set
            {
                frbObject = value;
                frbObjectAsScalable = value as global::FlatRedBall.Math.Geometry.IReadOnlyScalable;
            }
        }

        GraphicalUiElement GumParent { get; set; }
        public GraphicalUiElement GumObject { get; private set; }

        public PositionedObjectGueWrapper(PositionedObject frbObject, GraphicalUiElement gumObject) : base()
        {

            var renderable = new InvisibleRenderable();
            renderable.Visible = true;

            GumParent = new GraphicalUiElement();
            GumParent.SetContainedObject(renderable);
            GumParent.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
            GumParent.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;

            GumParent.XOrigin = HorizontalAlignment.Center;
            GumParent.YOrigin = VerticalAlignment.Center;


            this.FrbObject = frbObject;
            this.GumObject = gumObject;
            
            gumObject.Parent = GumParent;
        }

        public override void ForceUpdateDependencies()
        {
            base.ForceUpdateDependencies();
            UpdateGumObject();

        }

        public override void ForceUpdateDependenciesDeep()
        {
            base.ForceUpdateDependenciesDeep();
            UpdateGumObject();
        }

        public override void UpdateDependencies(double currentTime)
        {
            base.UpdateDependencies(currentTime);
            UpdateGumObject();
        }

        private void UpdateGumObject()
        {

            // This is going to get positioned according to the FRB object. I guess we'll force update dependencies, which is expensive...
            FrbObject.ForceUpdateDependencies();

            // todo - need to support multiple cameras and layers
            var camera = global::FlatRedBall.Camera.Main;


            int screenX = 0;
            int screenY = 0;

            var worldPosition = FrbObject.Position;

            global::FlatRedBall.Math.MathFunctions.AbsoluteToWindow(
                worldPosition.X, worldPosition.Y, worldPosition.Z,
                ref screenX, ref screenY, camera);

            var zoom = 1.0f;
            if (camera.Orthogonal)
            {
                zoom = camera.DestinationRectangle.Height / camera.OrthogonalHeight;
            }

            GumParent.X = screenX / zoom;
            GumParent.Y = screenY / zoom;

            if(this.ParentRotationChangesRotation)
            {
                GumParent.Rotation = Microsoft.Xna.Framework.MathHelper.ToDegrees(this.FrbObject.RotationZ);
            }

            if (frbObjectAsScalable != null)
            {
                GumParent.Width = frbObjectAsScalable.ScaleX * 2;
                GumParent.Height = frbObjectAsScalable.ScaleY * 2;
            }
        }

        public Vector3 GetAbsolutePositionInFrbSpace(GraphicalUiElement graphicalUiElement)
        {
            var parentX = GumObject.GetAbsoluteX();
            var parentY = GumObject.GetAbsoluteY();

            var gumObjectAsIpso = GumObject as IPositionedSizedObject;

            var rectX = graphicalUiElement.GetAbsoluteX();
            var rectY = graphicalUiElement.GetAbsoluteY();

            var rectLeftOffset = rectX - parentX;
            var rectTopOffset = rectY - parentY;

            var toReturn = new Vector3();
            
            toReturn.X = FrbObject.X + gumObjectAsIpso.X + rectLeftOffset
                + graphicalUiElement.Width / 2.0f;
            toReturn.Y = FrbObject.Y - gumObjectAsIpso.Y - rectTopOffset
                - graphicalUiElement.Height / 2.0f;

            toReturn.Z = FrbObject.Z;

            return toReturn;
        }
    }
}
