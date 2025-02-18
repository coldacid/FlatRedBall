﻿using FlatRedBall.Glue.Errors;
using FlatRedBall.Glue.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FlatRedBall.Glue.Errors
{
    public class GlueErrorManager
    {
        public List<IErrorReporter> ErrorReporters { get; private set; } = new List<IErrorReporter>();

        List<ErrorViewModel> errors = new List<ErrorViewModel>();

        public IEnumerable<ErrorViewModel> Errors => errors;

        public void Add(IErrorReporter errorReporter) => ErrorReporters.Add(errorReporter);

        public void Add(ErrorViewModel error)
        {
            var isAlreadyReferenced = errors.Any(item => item.UniqueId == error.UniqueId);

            if(!isAlreadyReferenced)
            {
                errors.Add(error);
            }

            // Vic says - I don't like this. I think maybe this should get moved out of a plugin?
            // Need to think about it a bit...
            PluginManager.CallPluginMethod("Error Window Plugin", "RefreshErrors");
        }

        public void ClearFixedErrors() => errors.RemoveAll(item => item.GetIfIsFixed());
    }
}
