﻿using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(PurdueIo.Startup))]

namespace PurdueIo
{
	public partial class Startup
	{
		public void Configuration(IAppBuilder app)
		{
		}
	}
}
