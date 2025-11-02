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
        private float _scale = 9;
        private float _prevX;
        private float _prevY;
        private float _prevZ;
        private float _xAngle;
        private float _yAngle;
        private float _zAngle;
        private float _tX;
        private float _tY;
        private float _tZ;

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

        public ReactiveCommand<Unit, Unit> ScaleUpCommand { get; set; }
        public ReactiveCommand<Unit, Unit> ScaleDownCommand { get; set; }
        public ReactiveCommand<Unit, Unit> TranslateCommand { get; set; }

        private readonly ReadOnlyObservableCollection<Outline> _lines;
        public ReadOnlyObservableCollection<Outline> Lines => _lines;

        private readonly FigureService _figureService;

        public MainViewModel(FigureService figureService)
        {
            _figureService = figureService;

            _figureService.Lines
                .ObserveOn(RxApp.MainThreadScheduler)
                .Bind(out _lines)
                .Subscribe();

            ScaleUpCommand = ReactiveCommand.Create(() =>
            {
                if (Scale < 1)
                    Scale = 1;
                else
                    Scale += 1;
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
                _figureService.AddMatrix(new Vector3(TranslateX, TranslateY, TranslateZ));
            });

            this.WhenAnyValue(x => x.Scale)
                .Subscribe(x => { _figureService.AddMatrix(Scale); });

            this.WhenAnyValue(x => x.XAngle)
                .Subscribe(x =>
                {
                    var delta = x - _prevX;
                    _prevX = x;
                    _figureService.AddMatrix(delta, Vector4.UnitX);
                });

            this.WhenAnyValue(x => x.YAngle)
                .Subscribe(x =>
                {
                    var delta = x - _prevY;
                    _prevY = x;
                    _figureService.AddMatrix(delta, Vector4.UnitY);
                });

            this.WhenAnyValue(x => x.ZAngle)
                .Subscribe(x =>
                {
                    var delta = x - _prevZ;
                    _prevZ = x;
                    _figureService.AddMatrix(delta, Vector4.UnitZ);
                });

            _figureService.Build();
        }
    }
}
