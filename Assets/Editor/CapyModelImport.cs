using UnityEditor;

// Setting import untuk FBX karakter di Assets/Art/Models (sesuai README CapyQuest_Assets):
// skala 1, Bake Axis Conversion, material dari MaterialDescription, tanpa animasi/kamera/lampu.
public class CapyModelImport : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        if (!assetPath.StartsWith(CapyArt.ModelFolder + "/"))
            return;
        ModelImporter mi = (ModelImporter)assetImporter;
        mi.globalScale = 1f;
        mi.bakeAxisConversion = true;
        mi.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
        mi.animationType = ModelImporterAnimationType.None;
        mi.importAnimation = false;
        mi.importCameras = false;
        mi.importLights = false;
    }
}
