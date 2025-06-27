using System.Runtime.CompilerServices;
using JLChnToZ.VRC.Foundation;
using JLChnToZ.VRC.Foundation.I18N;

[assembly: InternalsVisibleTo("JLChnToZ.VRC.Foundation.Editor")]
[assembly: EditorI18NSource(LanguageAssetPath = "Packages/idv.jlchntoz.vrcw-foundation/Resources/editor-lang.json")]

#if VRCSDK_3_7_0_OR_NEWER
[assembly: DeclareDefine("VRCSDK_3_7_0_OR_NEWER")]
#endif
#if VRCSDK_3_8_1_OR_NEWER
[assembly: DeclareDefine("VRCSDK_3_8_1_OR_NEWER")]
#endif