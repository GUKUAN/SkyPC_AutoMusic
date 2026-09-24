using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SkyPC_AutoMusic.Theme
{
    //挂在元素上的轻量动效：按压缩放、悬停浮起
    //每个元素用自己的 TransformGroup，互不影响
    public static class UiMotion
    {
        private static readonly QuadraticEase EaseOut = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        public static readonly DependencyProperty PressScaleProperty = DependencyProperty.RegisterAttached(
            "PressScale", typeof(bool), typeof(UiMotion), new PropertyMetadata(false, OnPressScaleChanged));

        public static bool GetPressScale(DependencyObject obj) { return (bool)obj.GetValue(PressScaleProperty); }
        public static void SetPressScale(DependencyObject obj, bool value) { obj.SetValue(PressScaleProperty, value); }

        public static readonly DependencyProperty HoverLiftProperty = DependencyProperty.RegisterAttached(
            "HoverLift", typeof(bool), typeof(UiMotion), new PropertyMetadata(false, OnHoverLiftChanged));

        public static bool GetHoverLift(DependencyObject obj) { return (bool)obj.GetValue(HoverLiftProperty); }
        public static void SetHoverLift(DependencyObject obj, bool value) { obj.SetValue(HoverLiftProperty, value); }

        private static void OnPressScaleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = d as UIElement;
            if (element == null)
                return;

            if ((bool)e.NewValue)
            {
                element.PreviewMouseLeftButtonDown += OnPressDown;
                element.PreviewMouseLeftButtonUp += OnPressUp;
                element.MouseLeave += OnPressUp;
            }
            else
            {
                element.PreviewMouseLeftButtonDown -= OnPressDown;
                element.PreviewMouseLeftButtonUp -= OnPressUp;
                element.MouseLeave -= OnPressUp;
            }
        }

        private static void OnHoverLiftChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            UIElement element = d as UIElement;
            if (element == null)
                return;

            if ((bool)e.NewValue)
            {
                element.MouseEnter += OnHoverEnter;
                element.MouseLeave += OnHoverLeave;
            }
            else
            {
                element.MouseEnter -= OnHoverEnter;
                element.MouseLeave -= OnHoverLeave;
            }
        }

        private static void OnPressDown(object sender, MouseButtonEventArgs e)
        {
            AnimateScale((UIElement)sender, 0.96, 120);
        }

        private static void OnPressUp(object sender, MouseEventArgs e)
        {
            AnimateScale((UIElement)sender, 1.0, 160);
        }

        private static void OnHoverEnter(object sender, MouseEventArgs e)
        {
            AnimateLift((UIElement)sender, -2, 160);
        }

        private static void OnHoverLeave(object sender, MouseEventArgs e)
        {
            AnimateLift((UIElement)sender, 0, 200);
        }

        private static void AnimateScale(UIElement element, double to, int milliseconds)
        {
            ScaleTransform scale = EnsureTransforms(element).Scale;
            DoubleAnimation animation = new DoubleAnimation(to, TimeSpan.FromMilliseconds(milliseconds)) { EasingFunction = EaseOut };
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private static void AnimateLift(UIElement element, double to, int milliseconds)
        {
            TranslateTransform translate = EnsureTransforms(element).Translate;
            DoubleAnimation animation = new DoubleAnimation(to, TimeSpan.FromMilliseconds(milliseconds)) { EasingFunction = EaseOut };
            translate.BeginAnimation(TranslateTransform.YProperty, animation);
        }

        //保证元素有一个自己的 TransformGroup（Scale + Translate）
        private static MotionTransforms EnsureTransforms(UIElement element)
        {
            TransformGroup group = element.RenderTransform as TransformGroup;
            if (group != null && group.Children.Count >= 2
                && group.Children[0] is ScaleTransform && group.Children[1] is TranslateTransform)
            {
                return new MotionTransforms((ScaleTransform)group.Children[0], (TranslateTransform)group.Children[1]);
            }

            ScaleTransform scale = new ScaleTransform(1, 1);
            TranslateTransform translate = new TranslateTransform(0, 0);
            TransformGroup created = new TransformGroup();
            created.Children.Add(scale);
            created.Children.Add(translate);
            element.RenderTransform = created;
            element.RenderTransformOrigin = new Point(0.5, 0.5);
            return new MotionTransforms(scale, translate);
        }

        private struct MotionTransforms
        {
            public readonly ScaleTransform Scale;
            public readonly TranslateTransform Translate;

            public MotionTransforms(ScaleTransform scale, TranslateTransform translate)
            {
                Scale = scale;
                Translate = translate;
            }
        }
    }
}
