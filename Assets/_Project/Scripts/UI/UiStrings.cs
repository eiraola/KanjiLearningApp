namespace Kanji.UI
{
    public static class UiStrings
    {
        public const string AppTitle = "Kanji Learning";
        public const string LoadStories = "Cargar historias";
        public const string ReadStories = "Leer historias";
        public const string DifficultKanjis = "Kanjis complicados";
        public const string EmptyDifficult = "Aún no has añadido kanjis";
        public const string AddToDifficult = "Añadir a complicados";
        public const string InDifficult = "Ya en complicados";
        public const string AddedToDifficult = "Añadido a complicados";
        public const string RemoveFromDifficult = "Eliminar de la lista";
        public const string RemovedFromDifficult = "Eliminado de complicados";
        public const string DifficultSaveFailed = "No se pudo guardar.";
        public const string RemoveFailed = "No se pudo eliminar.";
        public const string StudyKanji = "Estudiar kanjis";
        public const string Resolve = "Resolver";
        public const string StudySaveFailed = "No se pudo guardar el progreso.";
        public const string Quit = "Salir";
        public const string UnderConstruction = "Pantalla en construcción";
        public const string Back = "<";

        public const string PickFile = "Elegir archivo de texto";
        public const string LoadHint = "Elige un archivo .txt con tus historias.";
        public const string EncodingUtf8 = "UTF-8";
        public const string EncodingUtf8Bom = "UTF-8 con BOM";
        public const string EncodingShiftJis = "Shift-JIS";
        public const string FileMeta = "{0} KB · {1}";
        public const string MoreChars = "… ({0} caracteres más)";
        public const string ErrorTooLarge = "El archivo supera 1 MB. Divídelo en archivos más pequeños.";
        public const string ErrorEncoding = "Codificación no soportada, guarda el archivo como UTF-8";
        public const string ErrorUnreadable = "No se pudo leer el archivo.";

        public const string EmptyLibrary = "No hay historias. Ve a Cargar historias";
        public const string LibraryLoadFailed = "No se pudo cargar la biblioteca.";

        public const string Read = "Leer";
        public const string StoryOpenFailed = "No se pudo abrir la historia.";
        public const string ReaderLoading = "Cargando…";
        public const string Delete = "Eliminar";
        public const string Close = "Cerrar";
        public const string Knew = "Lo sabía";
        public const string DidntKnow = "No lo sabía";
        public const string FieldReading = "Lectura";
        public const string FieldMeaning = "Significado";
        public const string FieldSentenceMeaning = "Traducción";
        public const string Cancel = "Cancelar";

        public const string StatsTitle = "Resultados";
        public const string StatsTotal = "Total";
        public const string StatsPassed = "Acertados";
        public const string StatsFailed = "Fallados";
        public const string StatsFailedKanjis = "Kanjis fallados";
        public const string StatsNoFailed = "¡Ningún fallo!";
        public const string Accept = "Aceptar";
        public const string AlsoDeleteDifficult = "También se borrarán sus {0} kanjis complicados.";
        public const string DifficultMigrationDiscarded = "{0} kanjis complicados no se pudieron asignar y se descartaron.";
        public const string ConfirmDelete ="¿Eliminar '{0}'? No se puede deshacer.";
        public const string StoryDeleted = "Historia '{0}' eliminada";
        public const string DeleteFailed = "No se pudo eliminar la historia.";

        public const string NameTitle = "Nombre de la historia";
        public const string EditNameTitle = "Editar nombre";
        public const string EditName = "Editar nombre";
        public const string NameDuplicate = "Ya existe una historia llamada '{0}'. Elige otro nombre.";
        public const string StatKanjis = "Kanjis: {0}";
        public const string StatCharacters = "Caracteres: {0}";
        public const string StatWarnings = "Avisos: {0}";
        public const string NameUpdated = "Nombre actualizado";
        public const string StoryLoaded = "Historia '{0}' cargada: {1}";
        public const string SaveFailed = "No se pudo guardar la historia.";

        public static string ParseWarnings(int n) =>
            n == 1 ? "1 aviso al analizar el archivo"
                   : n.ToString(System.Globalization.CultureInfo.InvariantCulture) + " avisos al analizar el archivo";

        public static string Percent(int n) =>
            n.ToString(System.Globalization.CultureInfo.InvariantCulture) + " %";

        public static string KanjiCount(int n) =>
            n == 1 ? "1 kanji" : n.ToString(System.Globalization.CultureInfo.InvariantCulture) + " kanjis";

        public static string DifficultCount(int n) =>
            n == 1 ? "1 complicado"
                   : n.ToString(System.Globalization.CultureInfo.InvariantCulture) + " complicados";

        public static string UnreadableStories(int n) =>
            n == 1 ? "1 historia no se pudo leer"
                   : n.ToString(System.Globalization.CultureInfo.InvariantCulture) + " historias no se pudieron leer";
    }
}
