﻿using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.CodeGeneration.CodeBuilder;
using FlatRedBall.Glue.Elements;
using FlatRedBall.Glue.Parsing;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Math;
using OfficialPlugins.CollisionPlugin.Managers;
using OfficialPlugins.CollisionPlugin.ViewModels;
using OfficialPluginsCore.CollisionPlugin.ExtensionMethods;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OfficialPlugins.CollisionPlugin
{
    public class CollisionCodeGenerator : ElementComponentCodeGenerator
    {
        public override ICodeBlock GenerateInitializeLate(ICodeBlock codeBlock, IElement element)
        {
            //var collisionAti = AssetTypeInfoManager.Self.CollisionRelationshipAti;

            //var collisionRelationships = element.AllNamedObjects
            //    .Where(item => item.GetAssetTypeInfo() == collisionAti &&
            //        item.IsDisabled == false &&
            //        item.DefinedByBase == false &&
            //        item.SetByDerived == false)
            //    .ToArray();

            //foreach (var namedObject in collisionRelationships)
            //{
            //    GenerateInitializeCodeFor(namedObject, codeBlock);
            //}


            return codeBlock;
        }

        public static void GenerateInitializeCodeFor(NamedObjectSave namedObject, ICodeBlock codeBlock)
        {
            var firstCollidable = namedObject.GetFirstCollidableObjectName();
            var secondCollidable = namedObject.GetSecondCollidableObjectName();

            //////////////Early Out/////////////////////
            if (string.IsNullOrEmpty(firstCollidable))
            {
                return;
            }


            ///////////End Early Out///////////////////

            T Get<T>(string name) => namedObject.Properties.GetValue<T>(name);

            var collisionType = (CollisionType)Get<int>(
                nameof(CollisionRelationshipViewModel.CollisionType));

            var firstMass = Get<float>(
                nameof(CollisionRelationshipViewModel.FirstCollisionMass))
                .ToString(CultureInfo.InvariantCulture) + "f";

            var secondMass = Get<float>(
                nameof(CollisionRelationshipViewModel.SecondCollisionMass))
                .ToString(CultureInfo.InvariantCulture) + "f";

            var elasticity = Get<float>(
                nameof(CollisionRelationshipViewModel.CollisionElasticity))
                .ToString(CultureInfo.InvariantCulture) + "f";

            var firstSubCollision = Get<string>(
                nameof(CollisionRelationshipViewModel.FirstSubCollisionSelectedItem));

            if (firstSubCollision == "<Entire Object>")
            {
                firstSubCollision = null;
            }

            var secondSubCollision = Get<string>(
                nameof(CollisionRelationshipViewModel.SecondSubCollisionSelectedItem));

            if (secondSubCollision == "<Entire Object>")
            {
                secondSubCollision = null;
            }

            var isCollisionActive = Get<bool>(
                nameof(CollisionRelationshipViewModel.IsCollisionActive));

            var collisionLimit = (FlatRedBall.Math.Collision.CollisionLimit) Get<int>(
                nameof(CollisionRelationshipViewModel.CollisionLimit));

            var listVsListLoopingMode = (FlatRedBall.Math.Collision.ListVsListLoopingMode)Get<int>(
                nameof(CollisionRelationshipViewModel.ListVsListLoopingMode));

            var groupPlatformerVariableName = Get<string>(nameof(CollisionRelationshipViewModel.GroundPlatformerVariableName));
            var airPlatformerVariableName = Get<string>(nameof(CollisionRelationshipViewModel.AirPlatformerVariableName));
            var afterDoubleJumpPlatformerVariableName = Get<string>(nameof(CollisionRelationshipViewModel.AfterDoubleJumpPlatformerVariableName));

            

            var instanceName = namedObject.InstanceName;

            bool isFirstList;
            var firstType = AssetTypeInfoManager.GetFirstGenericType(namedObject, out isFirstList);

            bool isSecondList;
            var secondType = AssetTypeInfoManager.GetSecondGenericType(namedObject, out isSecondList);

            var isFirstTileShapeCollection = firstType == "FlatRedBall.TileCollisions.TileShapeCollection";
            var isSecondTileShapeCollection = secondType == "FlatRedBall.TileCollisions.TileShapeCollection";

            var isFirstShapeCollection = firstType == "FlatRedBall.Math.Geometry.ShapeCollection";
            var isSecondShapeCollection = secondType == "FlatRedBall.Math.Geometry.ShapeCollection";

            var isAlwaysColliding = secondCollidable == null;

            bool shouldManuallyAddToCollisionManager = false;

            if (collisionType == CollisionType.PlatformerSolidCollision ||
                collisionType == CollisionType.PlatformerCloudCollision)
            {
                GeneratePlatformerCollision(codeBlock, firstCollidable, secondCollidable, firstSubCollision, collisionType, instanceName, isFirstList, firstType, isSecondList, secondType);
            }
            else if(collisionType == CollisionType.DelegateCollision)
            {
                if(isFirstList && isSecondList)
                {
                    codeBlock.Line($"{instanceName} = new FlatRedBall.Math.Collision.DelegateListVsListRelationship<{firstType}, {secondType}>(" +
                        $"{firstCollidable}, {secondCollidable});");
                }
                else if(isFirstList)
                {
                    codeBlock.Line($"{instanceName} = new FlatRedBall.Math.Collision.DelegateListVsSingleRelationship<{firstType}, {secondType}>(" +
                        $"{firstCollidable}, {secondCollidable});");
                }
                else if(isSecondList)
                {
                    codeBlock.Line($"{instanceName} = new FlatRedBall.Math.Collision.DelegateSingleVsListRelationship<{firstType}, {secondType}>(" +
                        $"{firstCollidable}, {secondCollidable});");
                }
                else
                {
                    codeBlock.Line($"{instanceName} = new FlatRedBall.Math.Collision.DelegateCollisionRelationshipBase<{firstType}, {secondType}>(" +
                        $"{firstCollidable}, {secondCollidable});");
                }

                shouldManuallyAddToCollisionManager = true;

            }
            else if(isSecondTileShapeCollection)
            {
                // same method used for both list and non-list
                codeBlock.Line($"{instanceName} = " +
                    $"FlatRedBall.Math.Collision.CollisionManagerTileShapeCollectionExtensions.CreateTileRelationship(" +
                    $"FlatRedBall.Math.Collision.CollisionManager.Self, " +
                    $"{firstCollidable}, {secondCollidable});");

            }
            else if(isAlwaysColliding)
            {
                codeBlock.Line($"{instanceName} = new FlatRedBall.Math.Collision.AlwaysCollidingListCollisionRelationship<{firstType}>({firstCollidable});");
                shouldManuallyAddToCollisionManager = true;
            }
            //else if(isSecondShapeCollection)
            //{
            //    codeBlock.Line($"{instanceName} = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(" +
            //        $"{firstCollidable});");
            //}
            else
            {
                codeBlock.Line($"{instanceName} = FlatRedBall.Math.Collision.CollisionManager.Self.CreateRelationship(" +
                    $"{firstCollidable}, {secondCollidable});");
            }

            if (shouldManuallyAddToCollisionManager)
            {
                codeBlock.Line($"FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Add({instanceName});");
            }

            if (!string.IsNullOrEmpty(firstSubCollision) && 
                firstSubCollision != CollisionRelationshipViewModel.EntireObject &&
                collisionType != CollisionType.PlatformerCloudCollision &&
                collisionType != CollisionType.PlatformerSolidCollision)
            {
                codeBlock.Line($"{instanceName}.SetFirstSubCollision(item => item.{firstSubCollision});");
            }
            if(!string.IsNullOrEmpty(secondSubCollision) && 
                secondSubCollision != CollisionRelationshipViewModel.EntireObject &&
                collisionType != CollisionType.PlatformerCloudCollision &&
                collisionType != CollisionType.PlatformerSolidCollision)
            {
                codeBlock.Line($"{instanceName}.SetSecondSubCollision(item => item.{secondSubCollision});");
            }

            if(isFirstList && isSecondList)
            {
                codeBlock.Line($"{instanceName}.CollisionLimit = FlatRedBall.Math.Collision.CollisionLimit.{collisionLimit};");

                // currently list vs list delegate collision doesn't support the looping mode:
                var supportsLoopingMode = collisionType != CollisionType.PlatformerSolidCollision &&
                    collisionType != CollisionType.PlatformerCloudCollision;
                if(supportsLoopingMode)
                {
                    codeBlock.Line($"{instanceName}.ListVsListLoopingMode = FlatRedBall.Math.Collision.ListVsListLoopingMode.{listVsListLoopingMode};");
                }
            }

            codeBlock.Line($"{instanceName}.Name = \"{instanceName}\";");



            switch(collisionType)
            {
                case CollisionType.NoPhysics:
                    // don't do anything
                    break;
                case CollisionType.MoveCollision:

                    codeBlock.Line($"{instanceName}.SetMoveCollision({firstMass}, {secondMass});");
                    break;
                case CollisionType.BounceCollision:
                    //var relationship = new FlatRedBall.Math.Collision.CollisionRelationship();
                    //relationship.SetBounceCollision(firstMass, secondMass, elasticity);
                    codeBlock.Line($"{instanceName}.SetBounceCollision({firstMass}, {secondMass}, {elasticity});");
                    break;
            }

            if(!isCollisionActive)
            {
                codeBlock.Line(
                    $"{instanceName}.{nameof(FlatRedBall.Math.Collision.CollisionRelationship.IsActive)} = false;");
            }

            if(!string.IsNullOrEmpty(groupPlatformerVariableName) ||
                !string.IsNullOrEmpty(airPlatformerVariableName) ||
                !string.IsNullOrEmpty(afterDoubleJumpPlatformerVariableName) )
            {
                string StrippedName(string nameWithCsv)
                {
                    if(nameWithCsv?.Contains(" in ") == true)
                    {
                        var index = nameWithCsv.IndexOf(" in ");
                        return nameWithCsv.Substring(0, index);
                    }
                    else
                    {
                        return nameWithCsv;
                    }
                }

                if(isAlwaysColliding)
                {
                    codeBlock.Line(
                        $"{instanceName}.CollisionOccurred += (first) =>");
                }
                else
                {
                    codeBlock.Line(
                        $"{instanceName}.CollisionOccurred += (first, second) =>");
                }
                var eventBlock = codeBlock.Block();

                string GetRightSide(string variableName)
                {
                    if(variableName == "<NULL>")
                    {
                        return "null";
                    }
                    else
                    {
                        return $"{firstType}.PlatformerValuesStatic[\"{StrippedName(variableName)}\"]";
                    }
                }

                if (!string.IsNullOrEmpty(groupPlatformerVariableName))
                {
                    eventBlock.Line(
                        $"first.GroundMovement = {GetRightSide(groupPlatformerVariableName)};");
                }
                if(!string.IsNullOrEmpty(airPlatformerVariableName) )
                {
                    eventBlock.Line(
                        $"first.AirMovement = {GetRightSide(airPlatformerVariableName)};");
                }
                if (!string.IsNullOrEmpty(afterDoubleJumpPlatformerVariableName))
                {
                    eventBlock.Line(
                        $"first.AfterDoubleJump = {GetRightSide(afterDoubleJumpPlatformerVariableName)};");
                }
                codeBlock.Line(";");

            }

        }

        private static void GeneratePlatformerCollision(ICodeBlock codeBlock, 
            string firstCollidable, string secondCollidable, 
            string firstSubCollision, CollisionType collisionType,
            string instanceName, bool isFirstList, string firstType, bool isSecondList, string secondType)
        {
            var block = codeBlock.Block();

            var effectiveFirstType = firstType;
            if (isFirstList)
            {
                effectiveFirstType = $"FlatRedBall.Math.PositionedObjectList<{firstType}>";
            }
            var effectiveSecondType = secondType;
            if (isSecondList)
            {
                effectiveSecondType = $"FlatRedBall.Math.PositionedObjectList<{secondType}>";
            }


            var relationshipType =
                $"FlatRedBall.Math.Collision.DelegateCollisionRelationship<{effectiveFirstType}, {effectiveSecondType}>";

            if (isFirstList && isSecondList)
            {
                relationshipType = $"FlatRedBall.Math.Collision.DelegateListVsListRelationship<{firstType}, {secondType}>";
            }
            else if(isFirstList)
            {
                relationshipType = $"FlatRedBall.Math.Collision.DelegateListVsSingleRelationship<{firstType}, {secondType}>";
            }
            else if(isSecondList)
            {
                relationshipType = $"FlatRedBall.Math.Collision.DelegateSingleVsListRelationship<{firstType}, {secondType}>";
            }

            block.Line($"var temp = new {relationshipType}({firstCollidable}, {secondCollidable});");
            block.Line($"var isCloud = {(collisionType == CollisionType.PlatformerCloudCollision).ToString().ToLowerInvariant()};");
            block.Line($"temp.CollisionFunction = (first, second) =>");
            block = block.Block();

            string whatToCollideAgainst = "second";

            if( !isFirstList && isSecondList)
            {
                if(collisionType == CollisionType.PlatformerCloudCollision || collisionType == CollisionType.PlatformerSolidCollision)
                {
                    block.Line($"return first.CollideAgainst({whatToCollideAgainst}, isCloud);");
                }
                else
                {
                    // list vs list is internally handled already
                    if(firstSubCollision == null)
                    {
                        // it's an icollidable probably
                        block.Line($"return first.CollideAgainst({whatToCollideAgainst}.Collision, isCloud);");
                    }
                    else
                    {
                        block.Line($"return first.CollideAgainst({whatToCollideAgainst}.Collision, first.{firstSubCollision}, isCloud);");
                    }

                }
            }
            else // even if the first is list, we don't loop because we use a collision relationship type that handles the looping internally
            {
                if (firstSubCollision == null)
                {
                    // assume it's a shape collection
                    block.Line($"return first.CollideAgainst({whatToCollideAgainst}, isCloud);");
                }
                else
                {
                    // assume it's a shape collection
                    block.Line($"return first.CollideAgainst({whatToCollideAgainst}, first.{firstSubCollision}, isCloud);");
                }
            }

            block = block.End();
            block.Line(";");

            block.Line("FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Add(temp);");
            block.Line($"{instanceName} = temp;");
            //CollisionManager.Self.Relationships.Add(PlayerVsSolidCollision);
        }

        public static bool CanBePartitioned(NamedObjectSave nos)
        {
            if (nos.IsList)
            {
                var genericType = nos.SourceClassGenericType;

                var entity = ObjectFinder.Self.GetEntitySave(genericType);

                // todo - what about inheritance? We may need to handle that here.
                return entity?.ImplementsICollidable == true;
            }

            return false;
        }

        public override ICodeBlock GenerateAddToManagers(ICodeBlock codeBlock, IElement element)
        {
            // we only care about the top-level
            foreach(var nos in element.NamedObjects)
            {
                if(CanBePartitioned(nos))
                {
                    T Get<T>(string propName) =>
                        nos.Properties.GetValue<T>(propName);

                    if (Get<bool>(nameof(CollidableNamedObjectRelationshipViewModel.PerformCollisionPartitioning)))
                    {
                        var sortAxis = Get<Axis>(nameof(CollidableNamedObjectRelationshipViewModel.SortAxis));
                        var sortEveryFrame = Get<bool> (nameof(CollidableNamedObjectRelationshipViewModel.IsSortListEveryFrameChecked));
                        var partitionWidthHeight = Get<float>(nameof(CollidableNamedObjectRelationshipViewModel.PartitionWidthHeight));

                        // fill in this line:
                        codeBlock.Line(
                            $"FlatRedBall.Math.Collision.CollisionManager.Self.Partition({nos.InstanceName}, FlatRedBall.Math.Axis.{sortAxis}, " +
                            $"{CodeParser.ConvertValueToCodeString(partitionWidthHeight)}, {sortEveryFrame.ToString().ToLowerInvariant()});");
                    }
                }
            }

            return codeBlock;
        }

        public override ICodeBlock GenerateDestroy(ICodeBlock codeBlock, IElement element)
        {
			if (element is ScreenSave)
			{
				codeBlock.Line("FlatRedBall.Math.Collision.CollisionManager.Self.Relationships.Clear();");
			}
			return codeBlock;
        }
    }
}
