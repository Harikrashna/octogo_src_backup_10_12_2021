﻿using Xamarin.Forms.Internals;

namespace CF.Octogo.Behaviors
{
    [Preserve(AllMembers = true)]
    public interface IAction
    {
        bool Execute(object sender, object parameter);
    }
}