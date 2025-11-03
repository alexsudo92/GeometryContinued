using DynamicData;
using Geometry.UI.Models;
using Geometry.UI.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Reactive;
using System.Reactive.Linq;

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

        public ReactiveCommand<Unit, Unit> ScaleUpCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ScaleDownCommand { get; set; }
        public ReactiveCommand<Unit, Unit> TranslateCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ShearCommand { get; set; }
        public ReactiveCommand<Unit, Unit> PerspectiveCommand { get; set; }


        private readonly ReadOnlyObservableCollection<FigureYaml> _figures;
        public ReadOnlyObservableCollection<FigureYaml> Figures => _figures;

        private ReadOnlyObservableCollection<Outline> _lines;
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

        private FigureService _figureService;
        private readonly YamlService _yamlService;

        public MainViewModel(YamlService yamlService, FigureService figureService)
        {
            _yamlService = yamlService;
            _figureService = figureService;

            _yamlService.Figures
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
                     IsAvailable = SelectedFigure is not null;
                     if (x is not null)
                         _figureService.UpdateFigureData(x.AsFigure());
                     ResetValues();
                 });

            this.WhenAnyValue(x => x.Scale)
                .Subscribe(x => { _figureService.AddScaleMatrix(Scale); });

            this.WhenAnyValue(x => x.XAngle)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(x =>
                {
                    var delta = x - _prevX;
                    _prevX = x;
                    _figureService.AddRotationMatrix(delta, Vector4.UnitX);
                });

            this.WhenAnyValue(x => x.YAngle)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(x =>
                {
                    var delta = x - _prevY;
                    _prevY = x;
                    _figureService.AddRotationMatrix(delta, Vector4.UnitY);
                });

            this.WhenAnyValue(x => x.ZAngle)
                .Throttle(TimeSpan.FromMilliseconds(100))
                .Subscribe(x =>
                {
                    var delta = x - _prevZ;
                    _prevZ = x;
                    _figureService.AddRotationMatrix(delta, Vector4.UnitZ);
                });

            ScaleUpCommand = ReactiveCommand.Create(() =>
            {
                if (Scale < 1)
                    Scale = 1;
                else
                    Scale += 0.1f;
            });

            ScaleDownCommand = ReactiveCommand.Create(() =>
            {
                if (Scale > 1)
                    Scale = 1;

                if (Scale <= 1)
                    Scale -= 0.1f;
            });

            TranslateCommand = ReactiveCommand.Create(() =>
            {
                _figureService.AddTranslationMatrix(new Vector3(TranslateX, TranslateY, TranslateZ));
            });

            ShearCommand = ReactiveCommand.Create(() =>
            {
                _figureService.AddShearMatrix(yx: Shear);
            });

            PerspectiveCommand = ReactiveCommand.Create(() =>
            {
                _figureService.Projection(Perspective);
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
