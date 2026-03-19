using DynamicData;
using Geometry.UI.Avalonia.Models;
using Geometry.UI.Avalonia.Services;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Numerics;
using System.Reactive;
using System.Threading;

namespace Geometry.UI.Avalonia.ViewModels;

public class MainViewModel : ReactiveObject
{
    private float _scale = 1f;
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
    private float _areaX1 = -700f;
    private float _areaY1 = -500f;
    private float _areaX2 = 700f;
    private float _areaY2 = 500f;
    private float _areaWidth;
    private float _areaHeight;
    private float _animationX;
    private float _animationY;
    private float _animationZ;
    private bool _showArea;

    private readonly ReadOnlyObservableCollection<FacePolygon> _polygons;
    private readonly ReadOnlyObservableCollection<FigureModel> _figures;
    private readonly ReadOnlyObservableCollection<FigureModel> _availableFigures;
    private FigureModel? _selectedFigure;
    private FigureModel? _selectedAvailableFigure;
    private bool _isAvailable;
    private CancellationTokenSource? _source;

    private readonly FigureService _figureService;

    public MainViewModel(FigureLoaderService yamlService, FigureService figureService)
    {
        _figureService = figureService;

        yamlService.Figures
            .SortBy(x => x.Id)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _availableFigures)
            .Subscribe();

        _figureService.Polygons
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _polygons)
            .Subscribe();

        _figureService.Figures
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out _figures)
            .Subscribe();

        this.WhenAnyValue(x => x.SelectedFigure)
            .Subscribe(x =>
            {
                ResetValues();
                IsAvailable = x is not null;
            });

        this.WhenAnyValue(x => x.XAngle)
            .Subscribe(x => RotateFigure(x, ref _prevX, Vector4.UnitX));
        this.WhenAnyValue(x => x.YAngle)
            .Subscribe(x => RotateFigure(x, ref _prevY, Vector4.UnitY));
        this.WhenAnyValue(x => x.ZAngle)
            .Subscribe(x => RotateFigure(x, ref _prevZ, Vector4.UnitZ));

        this.WhenAnyValue(x => x.AreaX1).Subscribe(x => AreaWidth = AreaX2 - x);
        this.WhenAnyValue(x => x.AreaY1).Subscribe(x => AreaHeight = AreaY2 - x);
        this.WhenAnyValue(x => x.AreaX2).Subscribe(x => AreaWidth = x - AreaX1);
        this.WhenAnyValue(x => x.AreaY2).Subscribe(x => AreaHeight = x - AreaY1);

        AddFigureCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedAvailableFigure is not null)
            {
                _figureService.AddFigure(SelectedAvailableFigure.Clone());
            }
        });

        RemoveFigureCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedFigure is not null)
            {
                _figureService.RemoveFigure(_figures.IndexOf(SelectedFigure));
            }
        });

        ChangeScaleCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedFigure is not null)
            {
                _figureService.ScaleFigure(_figures.IndexOf(SelectedFigure), Scale);
            }
        });

        TranslateCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedFigure is not null)
            {
                _figureService.TranslateFigure(_figures.IndexOf(SelectedFigure), new Vector3(TranslateX, TranslateY, TranslateZ));
            }
        });

        ShearCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedFigure is not null)
            {
                _figureService.ShearFigure(_figures.IndexOf(SelectedFigure), yx: Shear);
            }
        });

        PerspectiveCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedFigure is not null)
            {
                _figureService.ProjectFigureToX(_figures.IndexOf(SelectedFigure), Perspective);
            }
        });

        MoveToAreaCommand = ReactiveCommand.Create(() =>
        {
            if (SelectedFigure is not null)
            {
                _figureService.MoveToAndScale(_figures.IndexOf(SelectedFigure), AreaX1, AreaY1, AreaX2, AreaY2);
            }
        });

        StartAnimationCommand = ReactiveCommand.CreateFromTask(async () =>
        {
            _source = new CancellationTokenSource();

            if (SelectedFigure is not null)
            {
                await _figureService.SmoothTranslateFigure(
                    _figures.IndexOf(SelectedFigure),
                    new Vector2(AnimationX, AnimationY),
                    _source.Token);
            }
        });

        StopAnimationCommand = ReactiveCommand.Create(() => _source?.Cancel(), StartAnimationCommand.IsExecuting);
    }

    public float Scale { get => _scale; set => this.RaiseAndSetIfChanged(ref _scale, value); }
    public float XAngle { get => _xAngle; set => this.RaiseAndSetIfChanged(ref _xAngle, value); }
    public float YAngle { get => _yAngle; set => this.RaiseAndSetIfChanged(ref _yAngle, value); }
    public float ZAngle { get => _zAngle; set => this.RaiseAndSetIfChanged(ref _zAngle, value); }
    public float TranslateX { get => _tX; set => this.RaiseAndSetIfChanged(ref _tX, value); }
    public float TranslateY { get => _tY; set => this.RaiseAndSetIfChanged(ref _tY, value); }
    public float TranslateZ { get => _tZ; set => this.RaiseAndSetIfChanged(ref _tZ, value); }
    public float Shear { get => _shear; set => this.RaiseAndSetIfChanged(ref _shear, value); }
    public float Perspective { get => _perspective; set => this.RaiseAndSetIfChanged(ref _perspective, value); }
    public float AreaX1 { get => _areaX1; set => this.RaiseAndSetIfChanged(ref _areaX1, value); }
    public float AreaY1 { get => _areaY1; set => this.RaiseAndSetIfChanged(ref _areaY1, value); }
    public float AreaX2 { get => _areaX2; set => this.RaiseAndSetIfChanged(ref _areaX2, value); }
    public float AreaY2 { get => _areaY2; set => this.RaiseAndSetIfChanged(ref _areaY2, value); }
    public float AreaWidth { get => _areaWidth; set => this.RaiseAndSetIfChanged(ref _areaWidth, value); }
    public float AreaHeight { get => _areaHeight; set => this.RaiseAndSetIfChanged(ref _areaHeight, value); }
    public float AnimationX { get => _animationX; set => this.RaiseAndSetIfChanged(ref _animationX, value); }
    public float AnimationY { get => _animationY; set => this.RaiseAndSetIfChanged(ref _animationY, value); }
    public float AnimationZ { get => _animationZ; set => this.RaiseAndSetIfChanged(ref _animationZ, value); }
    public bool ShowArea { get => _showArea; set => this.RaiseAndSetIfChanged(ref _showArea, value); }
    public bool IsAvailable { get => _isAvailable; set => this.RaiseAndSetIfChanged(ref _isAvailable, value); }

    public ReadOnlyObservableCollection<FigureModel> AvaiableFigures => _availableFigures;
    public ReadOnlyObservableCollection<FigureModel> Figures => _figures;
    public ReadOnlyObservableCollection<FacePolygon> Polygons => _polygons;

    public FigureModel? SelectedFigure { get => _selectedFigure; set => this.RaiseAndSetIfChanged(ref _selectedFigure, value); }
    public FigureModel? SelectedAvailableFigure { get => _selectedAvailableFigure; set => this.RaiseAndSetIfChanged(ref _selectedAvailableFigure, value); }

    public ReactiveCommand<Unit, Unit> AddFigureCommand { get; }
    public ReactiveCommand<Unit, Unit> RemoveFigureCommand { get; }
    public ReactiveCommand<Unit, Unit> ChangeScaleCommand { get; }
    public ReactiveCommand<Unit, Unit> TranslateCommand { get; }
    public ReactiveCommand<Unit, Unit> ShearCommand { get; }
    public ReactiveCommand<Unit, Unit> PerspectiveCommand { get; }
    public ReactiveCommand<Unit, Unit> MoveToAreaCommand { get; }
    public ReactiveCommand<Unit, Unit> StartAnimationCommand { get; }
    public ReactiveCommand<Unit, Unit> StopAnimationCommand { get; }

    public void ResetValues()
    {
        _prevX = _prevY = _prevZ = 0f;
        XAngle = 0f;
        YAngle = 0f;
        ZAngle = 0f;
        Scale = 1f;
        TranslateX = 0f;
        TranslateY = 0f;
        TranslateZ = 0f;
        Shear = 0f;
        Perspective = 0f;
    }

    private void RotateFigure(float angle, ref float previousAngle, Vector4 axis)
    {
        if (angle == 0f)
        {
            previousAngle = 0f;
            return;
        }

        var delta = angle - previousAngle;
        previousAngle = angle;

        if (SelectedFigure is not null)
        {
            _figureService.RotateFigure(_figures.IndexOf(SelectedFigure), delta, axis);
        }
    }
}
