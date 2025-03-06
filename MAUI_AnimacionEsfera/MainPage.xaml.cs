using System.Numerics;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace MAUI_AnimacionEsfera
{
    public partial class MainPage : ContentPage
    {
        private const float Gravity = 0.2f;
        private const float BallSize = 30f;
        private const float NormalBounceFactor = 0.9f;
        private const float BottomBounceFactor = 1.1f;  // Rebote extra en la parte inferior

        private float ContainerRadius = 150f;
        private Vector2 velocity1 = new Vector2(10, -2);
        private Vector2 velocity2 = new Vector2(-11, 1); // Azul
        private Vector2 position1;
        private Vector2 position2;
        private Vector2 containerCenter;

        private SKBitmap trailBitmap;
        private SKCanvas trailCanvas;

        private readonly List<Vector2> trail = new List<Vector2>();
        private readonly IDispatcherTimer timer;

        public float ContainerDiameter { get => ContainerRadius * 2; }

        public MainPage()
        {
            InitializeComponent();
            BindingContext = this;

            InitializeTrailCanvas();
            InitializePositions();

            DrawCanvas.InvalidateSurface(); // Forzar dibujo inicial

            // Timer que actualiza la animación
            timer = Dispatcher.CreateTimer();
            timer.Interval = TimeSpan.FromMilliseconds(16); //16 = 60 FPS
            timer.Tick += (s, e) => Update();
            timer.Start();
        }
        

        private void InitializeTrailCanvas()
        {
            // Obtener el tamaño real del SKCanvasView (si ya está renderizado)
            var canvasSize = (float)Math.Min(DrawCanvas.Width, DrawCanvas.Height);
            if (canvasSize <= 0) canvasSize = 400; // Valor por defecto temporal

            trailBitmap = new SKBitmap((int)canvasSize, (int)canvasSize);
            trailCanvas = new SKCanvas(trailBitmap);
            trailCanvas.Clear(SKColors.Black);
        }

        private void InitializePositions()
        {
            double error = 15;
            double centerX = (MainGrid.Width / 2) + error;
            double centerY = (MainGrid.Height / 2) + error;
            containerCenter = new Vector2((float)centerX, (float)centerY);

            position1 = containerCenter + new Vector2(-50, -50);
            position2 = containerCenter + new Vector2(50, 50);

            Ball1.WidthRequest = Ball1.HeightRequest = BallSize;
            Ball2.WidthRequest = Ball2.HeightRequest = BallSize;
        }

        private void Update()
        {
            // Aplicar gravedad
            velocity1.Y += Gravity;
            velocity2.Y += Gravity;

            // Actualizar posiciones
            position1 += velocity1;
            position2 += velocity2;

            // Checar colisión con el contenedor
            HandleCollision(ref position1, ref velocity1);
            HandleCollision(ref position2, ref velocity2);

            float error = 50;

            // La bola roja dibuja su rastro
            DrawTrail(new Vector2(position1.X + error, position1.Y + error));

            // La bola azul borra su rastro al pasar
            EraseTrail(new Vector2(position2.X + error, position2.Y + error), BallSize);

            DrawCanvas.InvalidateSurface(); // Redibujar el lienzo
            Redraw();
        }

        private void HandleCollision(ref Vector2 pos, ref Vector2 vel)
        {
            Vector2 delta = pos - containerCenter;
            float distance = delta.Length();
            float maxDistance = ContainerRadius - (BallSize / 2); // Ajustar el borde exacto

            if (distance >= maxDistance)
            {
                Vector2 normal = Vector2.Normalize(delta);
                pos = containerCenter + normal * maxDistance; // Evita que se salga

                // Si la colisión es en la parte inferior del círculo, dar más rebote
                float bounceFactor = normal.Y > 0.5f ? BottomBounceFactor : NormalBounceFactor;

                // Aplicar rebote con el factor adecuado
                vel = Vector2.Reflect(vel, normal) * bounceFactor;

                // Mantener la velocidad dentro de un rango estable
                float speed = vel.Length();
                if (speed < 4) vel *= 1.6f; // Evita que se quede pegada
                if (speed > 10) vel *= 0.9f; // Evita que salga disparada
            }
        }

        private void Redraw()
        {
            Ball1.TranslationX = position1.X - BallSize / 2;
            Ball1.TranslationY = position1.Y - BallSize / 2;
            Ball2.TranslationX = position2.X - BallSize / 2;
            Ball2.TranslationY = position2.Y - BallSize / 2;
        }

        private void DrawTrail(Vector2 point)
        {
            using (SKPaint paint = new SKPaint
            {
                Color = SKColors.OrangeRed,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            })
            {
                // Ajustar coordenadas restando la posición del contenedor
                float adjustedX = point.X - (containerCenter.X - ContainerRadius);
                float adjustedY = point.Y - (containerCenter.Y - ContainerRadius);

                trailCanvas.DrawCircle(adjustedX, adjustedY, BallSize / 2, paint);
            }
        }

        private void EraseTrail(Vector2 point, float eraserSize)
        {
            using (SKPaint erasePaint = new SKPaint
            {
                BlendMode = SKBlendMode.Clear, // Borra con transparencia
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            })
            {
                float adjustedX = point.X - (containerCenter.X - ContainerRadius);
                float adjustedY = point.Y - (containerCenter.Y - ContainerRadius);

                trailCanvas.DrawCircle(adjustedX, adjustedY, eraserSize / 2, erasePaint);
            }
        }

        private void OnCanvasPaint(object sender, SKPaintSurfaceEventArgs e)
        {
            SKCanvas canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Black);
            canvas.DrawBitmap(trailBitmap, 0, 0);
        }
    }
}
