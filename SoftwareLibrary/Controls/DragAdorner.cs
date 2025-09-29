using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Controls;

namespace SoftwareLibrary.Controls
{
    // Simple adorner that draws a visual brush at an offset to follow the mouse during drag
    internal class DragAdorner : Adorner
    {
        private readonly UIElement _child;
        private double _leftOffset;
        private double _topOffset;

        public DragAdorner(UIElement adornedElement, UIElement adornVisual, double opacity = 0.7) : base(adornedElement)
        {
            var brush = new VisualBrush(adornVisual)
            {
                Opacity = opacity,
                Stretch = Stretch.None,
                AlignmentX = AlignmentX.Left,
                AlignmentY = AlignmentY.Top
            };

            var rect = new System.Windows.Shapes.Rectangle
            {
                Width = ((FrameworkElement)adornVisual).ActualWidth,
                Height = ((FrameworkElement)adornVisual).ActualHeight,
                Fill = brush,
                IsHitTestVisible = false
            };

            _child = rect;
            AddVisualChild(_child);
            IsHitTestVisible = false;
        }

        public void SetOffsets(double left, double top)
        {
            _leftOffset = left;
            _topOffset = top;
            InvalidateVisual();
        }

        protected override int VisualChildrenCount => 1;
        protected override Visual GetVisualChild(int index) => _child;

        protected override System.Windows.Size MeasureOverride(System.Windows.Size constraint)
        {
            _child.Measure(constraint);
            return _child.DesiredSize;
        }

        protected override System.Windows.Size ArrangeOverride(System.Windows.Size finalSize)
        {
            _child.Arrange(new System.Windows.Rect(new System.Windows.Point(_leftOffset, _topOffset), finalSize));
            return finalSize;
        }

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var result = new GeneralTransformGroup();
            result.Children.Add(base.GetDesiredTransform(transform));
            result.Children.Add(new TranslateTransform(_leftOffset, _topOffset));
            return result;
        }
    }
}
