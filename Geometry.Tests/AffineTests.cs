using Geometry.Base;
using Geometry.Base.Transformation;
using System.Numerics;
using Xunit.Abstractions;

namespace Geometry.Tests
{
    public class AffineTests
    {
        private readonly ITestOutputHelper output;

        public AffineTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void RotateAroundYAxis()
        {
            // Input
            var rnd = new Random();

            var x = rnd.Next(-5, 5);
            var y = rnd.Next(-5, 5);
            var z = rnd.Next(-5, 5);

            var p = new Vector4(x, y, z, 1);
            var angle = rnd.Next(0, 90);

            var rad = angle * MathF.PI / 180;
            var cos = MathF.Cos(rad);
            var sin = MathF.Sin(rad);

            // Manual
            var mx = p.X * cos + p.Y * 0 + p.Z * sin + p.W * 0;
            var my = p.X * 0 + p.Y + p.Z * 0 + p.W * 0;
            var mz = -(p.X * sin) + p.Y * 0 + p.Z * cos + p.W * 0;
            var mw = p.X * 0 + p.Y * 0 + p.Z * 0 + p.W * 1;

            var rotP = new Vector4(mx, my, mz, mw);

            // Calculated
            var calcP = new Point4D(x, y, z);
            var matrix = Affine.GetRotationMatrix(angle, Vector4.UnitY);
            calcP.Transform(matrix);
            var calcPV = calcP.Vector4;

            output.WriteLine($"{x} {y} {z} {angle}");
            output.WriteLine($"{mx} {my} {mz} {angle}");
            output.WriteLine($"{calcPV.X} {calcPV.Y} {calcPV.Z} {angle}");
            Assert.Equal(rotP, calcPV);
        }
    }
}
