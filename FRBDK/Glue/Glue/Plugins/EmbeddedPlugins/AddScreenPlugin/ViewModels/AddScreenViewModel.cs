﻿using FlatRedBall.Glue.MVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;

namespace GlueFormsCore.Plugins.EmbeddedPlugins.AddScreenPlugin.ViewModels
{
    #region AddScreenType Enum
    enum AddScreenType
    {
        LevelScreen,
        BaseLevelScreen,
        EmptyScreen
    }

    enum TmxOptions
    {
        NewStandardTmx,
        CopiedTmx,
        NoTmx
    }

    #endregion

    class AddScreenViewModel : ViewModel
    {
        #region General Values

        public AddScreenType AddScreenType
        {
            get => Get<AddScreenType>();
            set => Set(value);
        }

        public bool HasGameScreen
        {
            get => Get<bool>();
            set => Set(value);
        }

        #endregion

        #region Level Screen

        #region Level Screen Radio

        [DependsOn(nameof(HasGameScreen))]
        public Visibility LevelScreenOptionUiVisibility => HasGameScreen.ToVisibility();

        [DependsOn(nameof(AddScreenType))]
        public bool IsLevelScreen
        {
            get => AddScreenType == AddScreenType.LevelScreen;
            set
            {
                if(value)
                {
                    AddScreenType = AddScreenType.LevelScreen;
                }
            }
        }

        [DependsOn(nameof(AddScreenType))]
        [DependsOn(nameof(HasGameScreen))]
        public Visibility LevelScreenUiVisibility => (HasGameScreen && IsLevelScreen).ToVisibility();

        #endregion

        #region Screen Properties

        public bool InheritFromGameScreen
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsSetAsStartupChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        #endregion

        #region Entities

        [DependsOn(nameof(InheritFromGameScreen))]
        [DependsOn(nameof(AvailableLevels))]
        public Visibility LevelScreenEntitiesGroupVisibility => (InheritFromGameScreen && AvailableLevels.Count > 0).ToVisibility();

        public bool IsCopyEntitiesFromOtherLevelChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        [DependsOn(nameof(IsCopyEntitiesFromOtherLevelChecked))]
        public Visibility CopyEntitiesVisibility => IsCopyEntitiesFromOtherLevelChecked.ToVisibility();

        public ObservableCollection<string> AvailableLevels
        {
            get => Get< ObservableCollection<string>>();
            private set => Set(value);
        } 

        public string SelectedCopyEntitiesFromLevel
        {
            get => Get<string>();
            set => Set(value);
        }

        #endregion

        #region Tiled

        public TmxOptions TmxOptions
        {
            get => Get<TmxOptions>();
            set => Set(value);
        }



        [DependsOn(nameof(TmxOptions))]
        public bool IsAddStandardTmxChecked
        {
            get => TmxOptions == TmxOptions.NewStandardTmx;
            set
            {
                if (value) TmxOptions = TmxOptions.NewStandardTmx;
            }
        }

        [DependsOn(nameof(TmxOptions))]
        public bool IsCopyTmxFromOtherLevelChecked
        {
            get => TmxOptions == TmxOptions.CopiedTmx;
            set
            {
                if (value) TmxOptions = TmxOptions.CopiedTmx;
            }
        }
        [DependsOn(nameof(TmxOptions))]
        public bool IsNoTmxFileChecked
        {
            get => TmxOptions == TmxOptions.NoTmx;
            set
            {
                if (value) TmxOptions = TmxOptions.NoTmx;
            }
        }

        [DependsOn(nameof(AvailableTmxFiles))]
        public Visibility CopyTmxFromOtherLevelVisibility => (AvailableTmxFiles.Count > 0).ToVisibility();

        public ObservableCollection<string> AvailableTmxFiles { get; private set; } 

        public string SelectedTmxFile
        {
            get => Get<string>();
            set => Set(value);
        }

        [DependsOn(nameof(TmxOptions))]
        public Visibility TmxComboBoxVisibility => (TmxOptions == TmxOptions.CopiedTmx).ToVisibility();
        #endregion


        #endregion

        #region Game Screen (Base Level Screen)

        [DependsOn(nameof(HasGameScreen))]
        public Visibility GameScreenOptionUiVisibility => (!HasGameScreen).ToVisibility();

        [DependsOn(nameof(HasGameScreen))]
        public bool CanAddBaseLevelScreen => !HasGameScreen;

        [DependsOn(nameof(AddScreenType))]
        public bool IsBaseLevelScreen
        {
            get => AddScreenType == AddScreenType.BaseLevelScreen;
            set
            {
                if (value)
                {
                    AddScreenType = AddScreenType.BaseLevelScreen;
                }
            }
        }

        [DependsOn(nameof(AddScreenType))]
        public Visibility BaseLevelScreenUiVisibility => IsBaseLevelScreen.ToVisibility();

        public bool IsAddMapLayeredTileMapChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsAddSolidCollisionShapeCollectionChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsAddCloudCollisionShapeCollectionChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        public bool IsAddListsForEntitiesChecked
        {
            get => Get<bool>();
            set => Set(value);
        }

        #endregion

        #region Empty Screen

        [DependsOn(nameof(AddScreenType))]
        public bool IsEmptyScreen
        {
            get => AddScreenType == AddScreenType.EmptyScreen;
            set
            {
                if (value)
                {
                    AddScreenType = AddScreenType.EmptyScreen;
                }
            }
        }


        #endregion

        public AddScreenViewModel()
        {
            IsAddMapLayeredTileMapChecked = true;
            IsAddListsForEntitiesChecked = true;
            AvailableTmxFiles = new ObservableCollection<string>();
            AvailableLevels = new ObservableCollection<string>();
        }
    }
}
