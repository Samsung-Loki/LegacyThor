// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using TheAirBlow.Hreidmar.Avalonia.ViewModels;

namespace TheAirBlow.Hreidmar.Avalonia;

public class ViewLocator : IDataTemplate
{
    public IControl Build(object data)
    {
        var name = data.GetType().FullName!.Replace("ViewModel", "View");
        var type = Type.GetType(name);

        if (type != null)
        {
            return (Control) Activator.CreateInstance(type)!;
        }
        else
        {
            return new TextBlock {Text = "Not Found: " + name};
        }
    }

    public bool Match(object data)
    {
        return data is ViewModelBase;
    }
}