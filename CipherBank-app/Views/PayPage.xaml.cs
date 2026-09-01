// <copyright file="PayPage.xaml.cs" company="CipherBank">
// Copyright (c) CipherBank. All rights reserved.
// </copyright>

using CipherBank_app.ViewModels;

namespace CipherBank_app.Views;

/// <summary>Pay page.</summary>
public partial class PayPage : ContentPage
{
    private readonly PayViewModel _vm;

    public PayPage(PayViewModel vm)
    {
        InitializeComponent();
        _vm = vm;
        BindingContext = vm;
        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is nameof(PayViewModel.MixSources)
                or nameof(PayViewModel.MixTotal)
                or null)
            {
                MixBar.Sources = _vm.MixSources.ToList();
                MixBar.Total = _vm.MixTotal;
            }
        };
        MixBar.Sources = _vm.MixSources.ToList();
        MixBar.Total = _vm.MixTotal;
    }
}
