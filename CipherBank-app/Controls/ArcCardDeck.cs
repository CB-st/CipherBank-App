// <copyright file="ArcCardDeck.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using CipherBank_app.Animations;
using Microsoft.Maui.Dispatching;
using Microsoft.Maui.Layouts;

namespace CipherBank_app.Controls;

/// <summary>
/// An item-agnostic radial card deck: cards fan in along a curved path during a 1:1
/// finger drag and settle with a velocity-seeded damped spring. The centered card is
/// the focused (selected) item.
/// </summary>
public class ArcCardDeck : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(
        nameof(ItemsSource),
        typeof(IList),
        typeof(ArcCardDeck),
        propertyChanged: OnItemsSourceChanged);

    public static readonly BindableProperty ItemTemplateProperty = BindableProperty.Create(
        nameof(ItemTemplate),
        typeof(DataTemplate),
        typeof(ArcCardDeck),
        propertyChanged: OnItemTemplateChanged);

    public static readonly BindableProperty FocusedItemProperty = BindableProperty.Create(
        nameof(FocusedItem),
        typeof(object),
        typeof(ArcCardDeck),
        defaultBindingMode: BindingMode.TwoWay,
        propertyChanged: OnFocusedItemChanged);

    public static readonly BindableProperty DragFractionProperty = BindableProperty.Create(
        nameof(DragFraction),
        typeof(double),
        typeof(ArcCardDeck),
        0.0);

    public static readonly BindableProperty StrideProperty = BindableProperty.Create(
        nameof(Stride),
        typeof(double),
        typeof(ArcCardDeck),
        210.0,
        propertyChanged: OnTuningChanged);

    public static readonly BindableProperty MaxTiltProperty = BindableProperty.Create(
        nameof(MaxTilt),
        typeof(double),
        typeof(ArcCardDeck),
        52.0,
        propertyChanged: OnTuningChanged);

    public static readonly BindableProperty ArcDropProperty = BindableProperty.Create(
        nameof(ArcDrop),
        typeof(double),
        typeof(ArcCardDeck),
        40.0,
        propertyChanged: OnTuningChanged);

    private const double FlickThreshold = 2.0;        // index units / second
    private const double SnapDampingRatio = 0.75;     // underdamped
    private const double SnapAngularFrequency = 12.0; // rad / second
    private const int WindowSize = 2;                 // cards realized each side of center (blur is expensive)
    private const double FrameDt = 1.0 / 60.0;

    private readonly AbsoluteLayout _layout = new();
    private readonly List<View> _cards = new();
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

    private CarouselLayoutConfig _config;
    private double _position;
    private double _dragStartPosition;
    private double _lastTotalX;
    private double _lastSampleSeconds;
    private double _dragVelocity;
    private IDispatcherTimer? _springTimer;
    private bool _suppressFocusSync;

    public ArcCardDeck()
    {
        _config = BuildConfig();
        Content = _layout;

        var pan = new PanGestureRecognizer();
        pan.PanUpdated += OnPanUpdated;
        GestureRecognizers.Add(pan);
    }

    public IList? ItemsSource
    {
        get => (IList?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public object? FocusedItem
    {
        get => GetValue(FocusedItemProperty);
        set => SetValue(FocusedItemProperty, value);
    }

    /// <summary>
    /// 0 when a card is centered, rising toward 1 as the drag passes the midpoint between
    /// cards. Pages observe this to dim dependent panels proportionally while dragging.
    /// </summary>
    public double DragFraction
    {
        get => (double)GetValue(DragFractionProperty);
        private set => SetValue(DragFractionProperty, value);
    }

    public double Stride
    {
        get => (double)GetValue(StrideProperty);
        set => SetValue(StrideProperty, value);
    }

    public double MaxTilt
    {
        get => (double)GetValue(MaxTiltProperty);
        set => SetValue(MaxTiltProperty, value);
    }

    public double ArcDrop
    {
        get => (double)GetValue(ArcDropProperty);
        set => SetValue(ArcDropProperty, value);
    }

    private int Count => ItemsSource?.Count ?? 0;

    protected override void OnHandlerChanging(HandlerChangingEventArgs args)
    {
        base.OnHandlerChanging(args);
        if (args.NewHandler == null)
        {
            AbortSpring();
        }
    }

    private static void OnItemsSourceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var deck = (ArcCardDeck)bindable;

        if (oldValue is INotifyCollectionChanged oldIncc)
        {
            oldIncc.CollectionChanged -= deck.OnCollectionChanged;
        }

        if (newValue is INotifyCollectionChanged newIncc)
        {
            newIncc.CollectionChanged += deck.OnCollectionChanged;
        }

        deck.RebuildCards();
    }

    private static void OnItemTemplateChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ArcCardDeck)bindable).RebuildCards();

    private static void OnTuningChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var deck = (ArcCardDeck)bindable;
        deck._config = deck.BuildConfig();
        deck.ApplyLayout();
    }

    private static void OnFocusedItemChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var deck = (ArcCardDeck)bindable;
        if (deck._suppressFocusSync || deck.ItemsSource == null)
        {
            return;
        }

        int index = deck.ItemsSource.IndexOf(newValue);
        if (index >= 0 && Math.Abs(index - deck._position) > 0.001)
        {
            deck.AbortSpring();
            deck._position = index;
            deck.ApplyLayout();
        }
    }

    private CarouselLayoutConfig BuildConfig() =>
        CarouselLayoutConfig.Default with { Stride = Stride, MaxTilt = MaxTilt, ArcDrop = ArcDrop };

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => RebuildCards();

    private void RebuildCards()
    {
        AbortSpring();
        _layout.Children.Clear();
        _cards.Clear();

        if (ItemsSource == null || ItemTemplate == null)
        {
            return;
        }

        foreach (var item in ItemsSource)
        {
            var view = (View)ItemTemplate.CreateContent();
            view.BindingContext = item;
            AbsoluteLayout.SetLayoutFlags(view, AbsoluteLayoutFlags.PositionProportional);
            AbsoluteLayout.SetLayoutBounds(
                view,
                new Rect(0.5, 0.5, AbsoluteLayout.AutoSize, AbsoluteLayout.AutoSize));
            _layout.Children.Add(view);
            _cards.Add(view);
        }

        int focusIndex = FocusedItem != null ? ItemsSource.IndexOf(FocusedItem) : -1;
        _position = focusIndex >= 0 ? focusIndex : 0;
        ApplyLayout();
        CommitFocusedItem();
    }

    private void ApplyLayout()
    {
        for (int i = 0; i < _cards.Count; i++)
        {
            double d = i - _position;
            var card = _cards[i];

            bool visible = Math.Abs(d) <= WindowSize;
            card.IsVisible = visible;
            if (!visible)
            {
                continue;
            }

            var t = CarouselMath.ComputeCardTransform(d, _config);
            card.TranslationX = t.TranslationX;
            card.TranslationY = t.TranslationY;
            card.RotationY = t.RotationY;
            card.Scale = t.Scale;
            card.Opacity = t.Opacity;
            card.ZIndex = t.ZIndex;
        }
    }

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Started:
                AbortSpring();
                _dragStartPosition = _position;
                _lastTotalX = 0;
                _lastSampleSeconds = _stopwatch.Elapsed.TotalSeconds;
                _dragVelocity = 0;
                DragFraction = 0;
                break;

            case GestureStatus.Running:
                if (Count <= 1)
                {
                    break;
                }

                _position = SoftClamp(_dragStartPosition - (e.TotalX / _config.Stride));
                ApplyLayout();
                DragFraction = Math.Clamp(
                    Math.Abs(_position - Math.Round(_position, MidpointRounding.AwayFromZero)) * 2.0, 0, 1);

                double now = _stopwatch.Elapsed.TotalSeconds;
                double dt = now - _lastSampleSeconds;
                if (dt > 0)
                {
                    double deltaIndex = (e.TotalX - _lastTotalX) / _config.Stride;
                    _dragVelocity = -deltaIndex / dt; // finger-right (positive TotalX) lowers position
                    _lastTotalX = e.TotalX;
                    _lastSampleSeconds = now;
                }

                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (Count <= 1)
                {
                    break;
                }

                DragFraction = 0;
                int target = CarouselMath.ComputeTargetIndex(_position, _dragVelocity, Count, FlickThreshold);
                StartSpring(target, _dragVelocity);
                break;
        }
    }

    private double SoftClamp(double position)
    {
        double max = Math.Max(0, Count - 1);
        if (position < 0)
        {
            return position * 0.35;
        }

        if (position > max)
        {
            return max + ((position - max) * 0.35);
        }

        return position;
    }

    private void StartSpring(int target, double seedVelocity)
    {
        AbortSpring();

        if (MotionSettings.ReduceMotion)
        {
            _position = target;
            ApplyLayout();
            CommitFocusedItem();
            return;
        }

        double velocity = seedVelocity;
        double max = Math.Max(0, Count - 1);

        _springTimer = Dispatcher.CreateTimer();
        _springTimer.Interval = TimeSpan.FromSeconds(FrameDt);
        _springTimer.Tick += (_, _) =>
        {
            var state = CarouselMath.SpringStep(
                _position, velocity, target, FrameDt, SnapDampingRatio, SnapAngularFrequency);
            velocity = state.Velocity;
            _position = Math.Clamp(state.Position, 0, max); // hard clamp = firm edges, mid-list overshoot survives
            ApplyLayout();

            bool settled = Math.Abs(_position - target) < 0.001 && Math.Abs(velocity) < 0.001;
            if (settled)
            {
                _position = target;
                ApplyLayout();
                AbortSpring();
                CommitFocusedItem();
            }
        };
        _springTimer.Start();
    }

    private void AbortSpring()
    {
        _springTimer?.Stop();
        _springTimer = null;
    }

    private void CommitFocusedItem()
    {
        if (ItemsSource == null || Count == 0)
        {
            return;
        }

        int index = Math.Clamp((int)Math.Round(_position, MidpointRounding.AwayFromZero), 0, Count - 1);
        _suppressFocusSync = true;
        try
        {
            FocusedItem = ItemsSource[index];
        }
        finally
        {
            _suppressFocusSync = false;
        }
    }
}
