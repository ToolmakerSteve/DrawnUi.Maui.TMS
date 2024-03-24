﻿using AppoMobi.Maui.Gestures;

namespace Sandbox.Views.Controls;

public class DrawnTabsHeader : SkiaLayout
{
    #region PROPERTIES

    public static readonly BindableProperty ScrollAmountProperty = BindableProperty.Create(
        nameof(ScrollAmount),
        typeof(double),
        typeof(DrawnTabsHeader),
        0.0, propertyChanged: NeedUpdateOffset);

    public double ScrollAmount
    {
        get { return (double)GetValue(ScrollAmountProperty); }
        set { SetValue(ScrollAmountProperty, value); }
    }

    public static readonly BindableProperty SelectedIndexProperty = BindableProperty.Create(
        nameof(SelectedIndex),
        typeof(double),
        typeof(DrawnTabsHeader),
        0.0, propertyChanged: NeedUpdateOffset);

    public double SelectedIndex
    {
        get { return (double)GetValue(SelectedIndexProperty); }
        set { SetValue(SelectedIndexProperty, value); }
    }

    public static readonly BindableProperty TabsCountProperty = BindableProperty.Create(
        nameof(TabsCount),
        typeof(int),
        typeof(DrawnTabsHeader),
        1, propertyChanged: NeedUpdateLayout);

    public int TabsCount
    {
        get { return (int)GetValue(TabsCountProperty); }
        set { SetValue(TabsCountProperty, value); }
    }


    public static readonly BindableProperty ClipOffsetProperty = BindableProperty.Create(
        nameof(ClipOffset),
        typeof(double),
        typeof(DrawnTabsHeader),
        0.0);

    private static void NeedUpdateLayout(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (bindable is DrawnTabsHeader control)
        {
            control.AdaptLayout();
        }
    }

    private static void NeedUpdateOffset(BindableObject bindable, object oldvalue, object newvalue)
    {
        if (bindable is DrawnTabsHeader control)
        {
            control.UpdateOffset();
        }
    }

    public static readonly BindableProperty TouchEffectColorProperty = BindableProperty.Create(nameof(TouchEffectColor), typeof(Color),
        typeof(DrawnTabsHeader),
        Colors.White);
    public Color TouchEffectColor
    {
        get { return (Color)GetValue(TouchEffectColorProperty); }
        set { SetValue(TouchEffectColorProperty, value); }
    }

    #endregion

    #region GESTURES

    public override ISkiaGestureListener ProcessGestures(TouchActionType type, TouchActionEventArgs args, TouchActionResult touchAction,
        SKPoint childOffset, SKPoint childOffsetDirect, ISkiaGestureListener wasConsumed)
    {
        if (touchAction == TouchActionResult.Tapped)
        {

            var ptsInsideControl = GetOffsetInsideControlInPoints(args.Location, childOffset);
            this.PlayRippleAnimation(TouchEffectColor, ptsInsideControl.X, ptsInsideControl.Y);

            //apply transfroms
            var thisOffset = TranslateInputCoords(childOffset, true);

            //apply touch coords
            var x = (args.Location.X + thisOffset.X) / RenderingScale;

            int Index = Math.Min((int)(x / _ptsTabWidth), TabsCount - 1);

            //Trace.WriteLine($"[T] {Index}");

            if (Index >= 0 && Index < TabsCount)
                this.SelectedIndex = Index;

            return this;

        }

        return base.ProcessGestures(type, args, touchAction, childOffset, childOffsetDirect, wasConsumed);
    }

    #endregion

    void AdaptLayout()
    {
        _ptsTabWidth = this.Width / (double)TabsCount;

        UpdateOffset();

        Update();
    }

    protected override void OnLayoutChanged()
    {
        base.OnLayoutChanged();

        AdaptLayout();
    }

    private double _OffsetClip;
    public double OffsetClip
    {
        get
        {
            return _OffsetClip;
        }
        set
        {
            if (_OffsetClip != value)
            {
                _OffsetClip = value;
                OnPropertyChanged();
            }
        }
    }

    private SkiaControl _clip;
    private double _ptsTabWidth;

    public double ClipOffset
    {
        get { return (double)GetValue(ClipOffsetProperty); }
        set { SetValue(ClipOffsetProperty, value); }
    }

    protected override void OnChildAdded(SkiaControl child)
    {
        base.OnChildAdded(child);

        if (_clip == null)
        {
            _clip = FindView<SkiaShape>("Clip");
        }
    }

    private void UpdateOffset()
    {
        OffsetClip = (Width - _ptsTabWidth) * ScrollAmount;

        Update();
    }

    protected override int RenderViewsList(IEnumerable<SkiaControl> skiaControls,
        SkiaDrawingContext context,
        SKRect destination, float scale,
        bool debug = false)
    {
        var drawn = 0;
        foreach (var child in skiaControls)
        {
            if (child != null)
            {
                child.OnBeforeDraw(); //could set IsVisible or whatever inside
                if (child.CanDraw) //still visible 
                {
                    if (child.Tag == "Active" && _clip != null)
                    {
                        var clip = _clip.CreateClip(null, true);
                        child.Clipping = (path, dest) =>
                        {
                            path.Reset();
                            clip.Offset((float)(OffsetClip * scale), 0);

                            //Trace.WriteLine($"[C] offset {OffsetClip:0.0} tab {_ptsTabWidth} max {Width}");

                            path.AddPath(clip);
                        };
                    }
                    child.Render(context, destination, scale);
                    drawn++;
                }
            }
        }
        return drawn;
    }




}