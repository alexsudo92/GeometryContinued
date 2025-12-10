using System.Windows;
using System.Windows.Media.Media3D;

namespace Geometry.OctreeUI.Services
{
    public class CameraService
    {
        public readonly PerspectiveCamera Camera;
        private Point3D _cameraPosition;
        private Vector3D _cameraLookDirection;
        private Vector3D _cameraUpDirection;

        private bool _isRotating = false;
        private bool _isPanning = false;
        private Point _lastMousePosition;

        // Параметры чувствительности
        private readonly float _rotationSpeed = 0.1f;
        private readonly float _panSpeed = 0.001f;
        private readonly float _zoomSpeed = 1.1f;

        public CameraService()
        {
            _cameraPosition = new(20, 20, 20);
            _cameraLookDirection = new(-1, -1, -1);
            _cameraUpDirection = new(0, 1, 0);

            Camera = new PerspectiveCamera(
                _cameraPosition,
                _cameraLookDirection,
                _cameraUpDirection,
                45f
            );
        }

        //private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
        //{
        //    if (e.RightButton == MouseButtonState.Pressed)
        //    {
        //        _isRotating = true;
        //        _lastMousePosition = e.GetPosition(MainViewport);
        //        MainViewport.CaptureMouse();
        //        MainViewport.Cursor = Cursors.SizeAll;
        //    }
        //    else if (e.MiddleButton == MouseButtonState.Pressed)
        //    {
        //        _isPanning = true;
        //        _lastMousePosition = e.GetPosition(MainViewport);
        //        MainViewport.CaptureMouse();
        //        MainViewport.Cursor = Cursors.ScrollAll;
        //    }
        //}

        //private void Viewport_MouseUp(object sender, MouseButtonEventArgs e)
        //{
        //    _isRotating = false;
        //    _isPanning = false;
        //    MainViewport.ReleaseMouseCapture();
        //    MainViewport.Cursor = Cursors.Arrow;
        //}

        //private void Viewport_MouseMove(object sender, MouseEventArgs e)
        //{
        //    if (!_isRotating && !_isPanning) return;

        //    var currentPosition = e.GetPosition(MainViewport);
        //    var delta = currentPosition - _lastMousePosition;

        //    if (_isRotating)
        //    {
        //        RotateCamera(delta.X, delta.Y);
        //    }
        //    else if (_isPanning)
        //    {
        //        PanCamera(delta.X, delta.Y);
        //    }

        //    _lastMousePosition = currentPosition;
        //}

        //private void Viewport_MouseWheel(object sender, MouseWheelEventArgs e)
        //{

        //    ZoomCamera(e.Delta > 0f ? _zoomSpeed : 1.0f / _zoomSpeed);
        //}

        private void RotateCamera(double deltaX, double deltaY)
        {
            var lookDirection = _cameraLookDirection;
            var upDirection = _cameraUpDirection;

            var rightDirection = Vector3D.CrossProduct(lookDirection, upDirection);

            var rotationY = new Quaternion(upDirection, -deltaX * _rotationSpeed);
            var rotationX = new Quaternion(rightDirection, -deltaY * _rotationSpeed);
            var totalRotation = rotationX * rotationY;

            var rotationMatrix = new Matrix3D();
            rotationMatrix.Rotate(totalRotation);

            lookDirection = rotationMatrix.Transform(lookDirection);
            upDirection = rotationMatrix.Transform(upDirection);

            _cameraLookDirection = lookDirection;
            _cameraUpDirection = upDirection;

            UpdateCamera();
        }

        private void PanCamera(double deltaX, double deltaY)
        {
            var actualDirection = Vector3D.CrossProduct(_cameraLookDirection, _cameraUpDirection);
            actualDirection.Normalize();

            var actualUp = Vector3D.CrossProduct(actualDirection, _cameraLookDirection);
            actualUp.Normalize();

            var cameraDistance = ((Vector3D)_cameraPosition).Length;
            var panFactor = _panSpeed * cameraDistance;

            _cameraPosition -= actualDirection * deltaX * panFactor;
            _cameraPosition += actualUp * deltaY * panFactor;

            UpdateCamera();
        }

        private void ZoomCamera(double factor)
        {
            var currentDistance = ((Vector3D)_cameraPosition).Length;

            var lookDirection = _cameraLookDirection;
            lookDirection.Normalize();

            _cameraPosition += lookDirection * currentDistance * (factor - 1.0);

            UpdateCamera();
        }

        private void UpdateCamera()
        {
            Camera.Position = _cameraPosition;
            Camera.LookDirection = _cameraLookDirection;
            Camera.UpDirection = _cameraUpDirection;
        }

        private void ResetCamera(object sender, RoutedEventArgs e)
        {
            _cameraPosition = new(20, 20, 20);
            _cameraLookDirection = new(-1, -1, -1);
            _cameraUpDirection = new(0, 1, 0);

            UpdateCamera();
        }
    }
}
