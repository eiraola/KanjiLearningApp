namespace Kanji.UI
{
    public static class UiStrings
    {
        public const string AppTitle = "Kanji Learning";
        public const string LoadStories = "Cargar historias";
        public const string ReadStories = "Leer historias";
        public const string DifficultKanjis = "Kanjis complicados";
        public const string WordLists = "Listas";
        public const string EmptyWordLists = "Aún no tienes listas. Pulsa + para crear una.";
        public const string NewWordListTitle = "Nueva lista";
        public const string WordListNamePlaceholder = "Nombre de la lista";
        public const string WordListNameDuplicate = "Ya existe una lista llamada '{0}'. Elige otro nombre.";
        public const string WordListCreated = "Lista '{0}' creada";
        public const string WordListOpenFailed = "No se pudo abrir la lista.";
        public const string EmptyWordList = "Esta lista no tiene palabras. Añádelas desde el menú de arriba a la derecha.";
        public const string AddWords = "Añadir palabras";
        public const string AddWordsHint = "Pega líneas separadas por tabuladores. La primera línea es la cabecera: Word, Sentence, Reading, Meaning, SentenceMeaning, SentenceReading (en cualquier orden; las que falten quedan vacías).";
        public const string AddWordsPlaceholder = "Pega aquí el texto…";
        public const string Paste = "Pegar";
        public const string Add = "Añadir";
        public const string ClipboardEmpty = "El portapapeles está vacío.";
        public const string PasteEmpty = "Pega o escribe al menos una línea.";
        public const string PasteNoTabs = "No hay tabuladores en el texto. Usa el botón Pegar para copiarlo tal cual.";
        public const string DuplicatesTitle = "Palabras repetidas ({0})";
        public const string DuplicatesQuestion = "Ya están en la lista o se repiten en el texto. ¿Qué hago con ellas?";
        public const string Ignore = "Ignorar";
        public const string UpdateWords = "Actualizar";
        public const string FieldSentenceReading = "Lectura de la frase";
        public const string StudyWords = "Estudiar";
        public const string StudyReverse = "Aprendizaje inverso";
        public const string ClearDrawing = "Borrar";
        public const string AddToDifficultWords = "Añadir a complicadas";
        public const string InDifficultWords = "Ya en complicadas";
        public const string FilterAll = "Todas ({0})";
        public const string FilterDifficult = "Complicadas ({0})";
        public const string EmptyDifficultWords = "No hay palabras complicadas en esta lista. Márcalas desde su tarjeta.";
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

        public static string WordCount(int n) =>
            n == 1 ? "1 palabra" : n.ToString(System.Globalization.CultureInfo.InvariantCulture) + " palabras";

        public static string PasteNoWords(int invalidLines) =>
            invalidLines > 0
                ? "No se encontró ninguna palabra (" + invalidLines.ToString(System.Globalization.CultureInfo.InvariantCulture) + " líneas no válidas)."
                : "No se encontró ninguna palabra.";

        /// <summary>"8 añadidas · 1 actualizada · 2 líneas no válidas"; only non-zero parts.</summary>
        public static string ImportSummary(int added, int updated, int ignored, int invalidLines)
        {
            var c = System.Globalization.CultureInfo.InvariantCulture;
            var parts = new System.Collections.Generic.List<string>(4);
            if (added > 0 || (updated == 0 && ignored == 0)) parts.Add(added.ToString(c) + (added == 1 ? " añadida" : " añadidas"));
            if (updated > 0) parts.Add(updated.ToString(c) + (updated == 1 ? " actualizada" : " actualizadas"));
            if (ignored > 0) parts.Add(ignored.ToString(c) + (ignored == 1 ? " repetida ignorada" : " repetidas ignoradas"));
            if (invalidLines > 0) parts.Add(invalidLines.ToString(c) + (invalidLines == 1 ? " línea no válida" : " líneas no válidas"));
            return string.Join(" · ", parts);
        }

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
