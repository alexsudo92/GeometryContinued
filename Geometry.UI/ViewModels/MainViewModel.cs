using DynamicData;
using Geometry.UI.Models;
using Geometry.UI.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;

namespace Geometry.UI.ViewModels
{
    public class MainViewModel : ReactiveObject
    {
        private float _scale = 1;
        private float _prevX;
        private float _prevY;
        private float _prevZ;
        private float _xAngle;
        private float _yAngle;
        private float _zAngle;
        private float _tX;
        private float _tY;
        private float _tZ;
        private float _shear;
        private float _perspective;
        private float _areaX1 = -700;
        private float _areaY1 = -500;
        private float _areaX2 = 700;
        private float _areaY2 = 500;
        private float _areaWidth;
        private float _areaHeight;

        public float Scale
        {
            get => _scale;
            set => this.RaiseAndSetIfChanged(ref _scale, value);
        }
        public float XAngle
        {
            get => _xAngle;
            set => this.RaiseAndSetIfChanged(ref _xAngle, value);
        }
        public float YAngle
        {
            get => _yAngle;
            set => this.RaiseAndSetIfChanged(ref _yAngle, value);
        }
        public float ZAngle
        {
            get => _zAngle;
            set => this.RaiseAndSetIfChanged(ref _zAngle, value);
        }
        public float TranslateX
        {
            get => _tX;
            set => this.RaiseAndSetIfChanged(ref _tX, value);
        }
        public float TranslateY
        {
            get => _tY;
            set => this.RaiseAndSetIfChanged(ref _tY, value);
        }
        public float TranslateZ
        {
            get => _tZ;
            set => this.RaiseAndSetIfChanged(ref _tZ, value);
        }
        public float Shear
        {
            get => _shear;
            set => this.RaiseAndSetIfChanged(ref _shear, value);
        }
        public float Perspective
        {
            get => _perspective;
            set => this.RaiseAndSetIfChanged(ref _perspective, value);
        }
        public float AreaX1
        {
            get => _areaX1;
            set => this.RaiseAndSetIfChanged(ref _areaX1, value);
        }
        public float AreaY1
        {
            get => _areaY1;
            set => this.RaiseAndSetIfChanged(ref _areaY1, value);
        }
        public float AreaX2
        {
            get => _areaX2;
            set => this.RaiseAndSetIfChanged(ref _areaX2, value);
        }
        public float AreaY2
        {
            get => _areaY2;
            set => this.RaiseAndSetIfChanged(ref _areaY2, value);
        }
        public float AreaWidth
        {
            get => _areaWidth;
            set => this.RaiseAndSetIfChanged(ref _areaWidth, value);
        }
        public float AreaHeight
        {
            get => _areaHeight;
            set => this.RaiseAndSetIfChanged(ref _areaHeight, value);
        }


        public ReactiveCommand<Unit, Unit> ChangeScaleCommand { get; set; }
        public ReactiveCommand<Unit, Unit> TranslateCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ShearCommand { get; set; }
        public ReactiveCommand<Unit, Unit> PerspectiveCommand { get; set; }
        public ReactiveCommand<bool, Unit> ChangeAreaVisibilityCommand { get; set; }
        public ReactiveCommand<Unit, Unit> MoveToAreaCommand { get; set; }

        private readonly ReadOnlyObservableCollection<FigureYaml> _figures;
        public ReadOnlyObservableCollection<FigureYaml> Figures => _figures;

        private readonly ReadOnlyObservableCollection<Outline> _lines;
        public ReadOnlyObservableCollection<Outline> Lines => _lines;

        private FigureYaml _selectedFigure;
        public FigureYaml SelectedFigure
        {
            get => _selectedFigure;
            set => this.RaiseAndSetIfChanged(ref _selectedFigure, value);
        }

        private bool _isAvailable;
        public bool IsAvailable
        {
            get => _isAvailable;
            set => this.RaiseAndSetIfChanged(ref _isAvailable, value);
        }

        private Visibility _isAreaVisible = Visibility.Hidden;
        public Visibility IsAreaVisible
        {
            get => _isAreaVisible;
            set => this.RaiseAndSetIfChanged(ref _isAreaVisible, value);
        }

        private readonly FigureService _figureService;
        private readonly YamlService _yamlService;

        public MainViewModel(YamlService yamlService, FigureService figureService)
        {
            _yamlService = yamlService;
            _figureService = figureService;

            _yamlService.Figures
                .SortBy(x => x.Id)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _figures)
                .Subscribe();

            _figureService.Lines
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _lines)
                .Subscribe();

            this.WhenAnyValue(x => x.SelectedFigure)
                 .Subscribe(x =>
                 {
                     ResetValues();
                     IsAvailable = SelectedFigure is not null;
                     if (x is not null)
                         _figureService.SetFigureData(x.AsFigure());
                 });

            this.WhenAnyValue(x => x.Scale)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe(x => Scale = x);

            this.WhenAnyValue(x => x.XAngle)
                //.Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(x =>
                {
                    var delta = x - _prevX;
                    _prevX = x;
                    _figureService.RotateFigure(delta, Vector4.UnitX);
                });

            this.WhenAnyValue(x => x.YAngle)
                //.Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(x =>
                {
                    var delta = x - _prevY;
                    _prevY = x;
                    _figureService.RotateFigure(delta, Vector4.UnitY);
                });

            this.WhenAnyValue(x => x.ZAngle)
                //.Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(x =>
                {
                    var delta = x - _prevZ;
                    _prevZ = x;
                    _figureService.RotateFigure(delta, Vector4.UnitZ);
                });

            this.WhenAnyValue(x => x.AreaX1)
                .Subscribe(x => AreaWidth = this.AreaX2 - x);
            this.WhenAnyValue(x => x.AreaY1)
                .Subscribe(x => AreaHeight = this.AreaY2 - x);
            this.WhenAnyValue(x => x.AreaX2)
                .Subscribe(x => AreaWidth = x - this.AreaX1);
            this.WhenAnyValue(x => x.AreaY2)
                .Subscribe(x => AreaHeight = x - this.AreaY1);

            ChangeScaleCommand = ReactiveCommand.Create(() =>
            {
                _figureService.ScaleFigure(Scale);
            });

            TranslateCommand = ReactiveCommand.Create(() =>
            {
                _figureService.TranslateFigure(new Vector3(TranslateX, TranslateY, TranslateZ));
            });

            ShearCommand = ReactiveCommand.Create(() =>
            {
                _figureService.ShearFigure(yx: Shear);
            });

            PerspectiveCommand = ReactiveCommand.Create(() =>
            {
                _figureService.ProjectFigureToX(Perspective);
            });

            MoveToAreaCommand = ReactiveCommand.Create(() =>
            {
                _figureService.ScaleAndMoveTo(AreaX1, AreaY1, AreaX2, AreaY2);
            });

            ChangeAreaVisibilityCommand = ReactiveCommand.Create((bool isChecked) =>
            {
                if (isChecked)
                    IsAreaVisible = Visibility.Visible;
                else
                    IsAreaVisible = Visibility.Hidden;
            });
        }

        public void ResetValues()
        {
            XAngle = 0;
            YAngle = 0;
            ZAngle = 0;
            Scale = 1;
            TranslateX = 0;
            TranslateY = 0;
            TranslateZ = 0;
            Shear = 0;
        }
    }
}
