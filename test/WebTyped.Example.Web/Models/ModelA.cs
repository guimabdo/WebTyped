﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebTyped.Example.Web.Models {
	public class ModelA {
		public int Id { get; set; }
		public string Name { get; set; }
		public DateTime Birthday { get; set; }
		public bool IsOk { get; set; }
		public Kind1 Kind1 { get; set; }
		public Kind2 Kind2 { get; set; }
	}

	
}