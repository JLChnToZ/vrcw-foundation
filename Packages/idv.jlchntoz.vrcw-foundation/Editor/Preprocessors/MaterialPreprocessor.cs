using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using VRC.Udon;
using UdonSharp;
using UdonSharpEditor;

namespace JLChnToZ.VRC.Foundation.Editors {
    public class MaterialPreprocessor : IPreprocessor {

        public int Priority => 0;

        public void OnPreprocess(Scene scene) {

        }
    }
}
