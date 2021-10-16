﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FlatRedBall.Glue.CodeGeneration;
using FlatRedBall.Glue.Parsing;
using System.Windows.Forms;
using FlatRedBall.Glue.Factories;
using FlatRedBall.Glue.FormHelpers;
using FlatRedBall.Glue.Elements;
using Glue;
using FlatRedBall.Glue.Controls;
using FlatRedBall.Glue.SaveClasses;
using FlatRedBall.Glue.Plugins.ExportedImplementations;
using GlueFormsCore.Managers;
using GlueFormsCore.SetVariable.EntitySaves;

namespace FlatRedBall.Glue.SetVariable
{
    class EntitySaveSetPropertyLogic
    {
        internal void ReactToEntityChangedProperty(string changedMember, object oldValue)
        {
            EntitySave entitySave = GlueState.Self.CurrentEntitySave;

            #region BaseEntity changed

            if (changedMember == nameof(EntitySave.BaseEntity))
            {
                InheritanceManager.ReactToChangedBaseEntity(oldValue as string, entitySave);
            }

            #endregion

            #region CreatedByOtherEntities changed

            else if (changedMember == nameof(EntitySave.CreatedByOtherEntities))
            {
                CreatedByOtherEntitiesSetLogic.HandleCreatedByOtherEntitiesSet(entitySave);
            }

            #endregion

            #region PooledByFactory

            else if (changedMember == nameof(entitySave.PooledByFactory) && (bool)oldValue != entitySave.PooledByFactory)
            {
                if (entitySave.PooledByFactory)
                {
                    // We should ask the user
                    // if Glue should set the reset
                    // variables for all contained objects
                    string message = "Would you like to add reset variables for all contained objects (recommended)";

                    DialogResult result = MessageBox.Show(message, "Add reset variables?", MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        FactoryManager.Self.SetResetVariablesForEntitySave(entitySave);
                    }
                }
                else // user set it to false
                {
                    var hasResetVariables = entitySave.AllNamedObjects.Any(item => item.VariablesToReset?.Any() == true);
                    if(hasResetVariables)
                    {
                        string message = "Would you like to remove reset variables for all contained objects? Select 'Yes' if you added reset variables earlier for pooling";

                        var dialogResult = MessageBox.Show(message, "Remove reset variables?", MessageBoxButtons.YesNo);

                        if(dialogResult == DialogResult.Yes)
                        {
                            FactoryManager.Self.RemoveResetVariablesForEntitySave(entitySave);
                        }
                    }
                }

                FactoryCodeGenerator.AddGeneratedPerformanceTypes();
                FactoryCodeGenerator.UpdateFactoryClass(entitySave);
            }

            #endregion

            #region Click Broadcast
            // Vic says:  I don't think we need this anymore
            else if (changedMember == "ClickBroadcast")
            {
                if (string.IsNullOrEmpty((string)oldValue) &&
                    !entitySave.ImplementsIClickable
                    )
                {
                    // Let the user know that this won't do anything unless the entity implements IClickable
                    string message = "The Click Broadcast message will not be broadcasted unless this " +
                        "Entity is made IClickable.  Would you like to make it IClickable?";

                    DialogResult result =
                        MessageBox.Show(message, "Make IClickable?", MessageBoxButtons.YesNo);

                    if (result == DialogResult.Yes)
                    {
                        entitySave.ImplementsIClickable = true;

                    }
                }
            }
            #endregion

            #region ImplementsIWindow

            else if (changedMember == "ImplementsIWindow")
            {
                if (entitySave.ImplementsIWindow && !entitySave.ImplementsIVisible)
                {
                    MessageBox.Show("IWindows must also be IVisible.  Automatically setting Implements IVisible to true");

                    entitySave.ImplementsIVisible = true;
                }

                RegenerateAllContainersForNamedObjectsThatUseCurrentEntity();

            }

            #endregion

            #region ImplementsIVisible

            else if (changedMember == "ImplementsIVisible")
            {
                ReactToChangedImplementsIVisible(oldValue, entitySave);
            }

            #endregion

            #region ImplementsIClickable
            else if (changedMember == "ImplementsIClickable")
            {
                RegenerateAllContainersForNamedObjectsThatUseCurrentEntity();
            }

            #endregion

            #region ItemType

            else if (changedMember == "ItemType")
            {
                EntitySave itemTypeEntity = ObjectFinder.Self.GetEntitySave(entitySave.ItemType);

                if (itemTypeEntity != null)
                {
                    if (!itemTypeEntity.CreatedByOtherEntities)
                    {
                        MessageBox.Show("The Entity " + entitySave.ItemType + " must be \"Created By Other Entities\" to be used as an Item Type");
                        entitySave.ItemType = null;
                    }
                }

            }

            #endregion

            #region ClassName

            else if (changedMember == "ClassName")
            {
                List<NamedObjectSave> allNamedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(GlueState.Self.CurrentElement);

                var containers = new List<GlueElement>();

                foreach (NamedObjectSave nos in allNamedObjects)
                {
                    var element = nos.GetContainer();

                    if (!containers.Contains(element))
                    {
                        containers.Add(element);
                    }
                }

                foreach (var element in containers)
                {
                    GlueCommands.Self.GenerateCodeCommands.GenerateElementCode(element);
                }
            }

            #endregion
        }

        private static void ReactToChangedImplementsIVisible(object oldValue, EntitySave entitySave)
        {
            #region If the user turned IVisible off, see if there is a "Visible" Exposed Variable
            if (((bool)oldValue) == true)
            {
                CustomVariable variableToRemove = entitySave.GetCustomVariable("Visible");
                if (variableToRemove != null)
                {
                    List<string> throwawayList = new List<string>();

                    MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                    mbmb.MessageText = "This entity has a \"Visible\" variable exposed.  This variable is no longer valid.  What would you like to do?";
                    mbmb.AddButton("Remove this variable", DialogResult.Yes);
                    mbmb.AddButton("Keep this as a non-functional Variable (it will no longer control the object's visibility)", DialogResult.No);

                    DialogResult result = mbmb.ShowDialog(MainGlueWindow.Self);

                    if (result == DialogResult.Yes)
                    {
                        ProjectManager.RemoveCustomVariable(variableToRemove, throwawayList);
                    }
                    else
                    {
                        // No need to do anything
                    }
                }
            }
            #endregion

            #region If the user turned IVisible on, see if there are any NamedObjectSaves that reference Elements that are not IVisible

            if (entitySave.ImplementsIVisible)
            {
                foreach (NamedObjectSave nos in entitySave.AllNamedObjects)
                {
                    if (nos.SourceType == SourceType.Entity || nos.IsList)
                    {

                        EntitySave nosEntitySave = null;

                        if (nos.SourceType == SourceType.Entity)
                        {
                            nosEntitySave = ObjectFinder.Self.GetEntitySave(nos.SourceClassType);
                        }
                        else
                        {
                            nosEntitySave = ObjectFinder.Self.GetEntitySave(nos.SourceClassGenericType);
                        }

                        if (nosEntitySave != null && nosEntitySave.ImplementsIVisible == false)
                        {
                            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
                            mbmb.MessageText = entitySave + " implements IVisible, but its object " + nos + " does not.  Would would you like to do?";

                            mbmb.AddButton("Make " + nosEntitySave + " implement IVisible", DialogResult.Yes);
                            mbmb.AddButton("Ignore " + nos + " when setting Visible on " + entitySave, DialogResult.No);
                            mbmb.AddButton("Do nothing - this will likely cause compile errors so this must be fixed manually", DialogResult.Cancel);

                            DialogResult result = mbmb.ShowDialog(MainGlueWindow.Self);

                            if (result == DialogResult.Yes)
                            {
                                nosEntitySave.ImplementsIVisible = true;

                                GlueCommands.Self.GenerateCodeCommands
                                    .GenerateElementAndReferencedObjectCodeTask(nosEntitySave);
                            }
                            else if (result == DialogResult.No)
                            {
                                nos.IncludeInIVisible = false;
                            }
                            else if (result == DialogResult.Cancel)
                            {
                                // do nothing - the user better fix this!
                            }
                        }
                    }
                }
            }
            #endregion

            #region If it's a ScrollableEntityList, then the item it's using must also be an IVisible

            if (entitySave.ImplementsIVisible && entitySave.IsScrollableEntityList && !string.IsNullOrEmpty(entitySave.ItemType))
            {
                EntitySave itemTypeAsEntity = ObjectFinder.Self.GetEntitySave(entitySave.ItemType);

                if (itemTypeAsEntity != null && itemTypeAsEntity.ImplementsIVisible == false)
                {
                    MessageBox.Show("The item type " + itemTypeAsEntity.ToString() + " must also implement IVisible.  Glue will do this now");

                    itemTypeAsEntity.ImplementsIVisible = true;

                    // Gotta regen this thing
                    var entityForItem = ObjectFinder.Self.GetIElement(entitySave.ItemType);
                    CodeWriter.GenerateCode(entityForItem);
                }
            }

            #endregion
        }

        private static void RegenerateAllContainersForNamedObjectsThatUseCurrentEntity()
        {
            var namedObjects = ObjectFinder.Self.GetAllNamedObjectsThatUseElement(GlueState.Self.CurrentEntitySave);
            var elementsToGenerate = new List<GlueElement>();
            foreach (NamedObjectSave nos in namedObjects)
            {
                var element = nos.GetContainer();

                if (!elementsToGenerate.Contains(element))
                {
                    elementsToGenerate.Add(element);
                }
            }

            foreach (var element in elementsToGenerate)
            {
                CodeWriter.GenerateCode(element);
            }
        }

    }
}