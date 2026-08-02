using System;
using System.Collections.Generic;
using System.Text;

namespace CEvol.Core
{
	internal class ErrorsBag
	{
		struct Error
		{
			public string Layer;
			public string ErrorCode;
			public string Comment;
			public PositionInSources Pos;
		}

		public bool HasErrors => _errors.Count > 0;

		private List<Error> _errors = new();

		private Dictionary<string, string> _sources;

		public ErrorsBag(Dictionary<string, string> sources)
		{
			_sources = sources;
		}

		public void AddError(string layer, string errorCode, string comment, PositionInSources pos)
		{
			_errors.Add(new Error()
			{
				Layer = layer,
				ErrorCode = errorCode,
				Comment = comment,
				Pos = pos
			});
		}

		public string BuildErrorsMessage()
		{
			if (_errors.Count == 0)
				return string.Empty;

			// Кешируем строки файлов для быстрого доступа по номеру строки
			var parsedSources = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
			foreach (var (fileName, content) in _sources)
			{
				// Поддержка всех вариантов переноса строк (CRLF, LF)
				parsedSources[fileName] = content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
			}

			var builder = new StringBuilder();

			for (int i = 0; i < _errors.Count; i++)
			{
				var err = _errors[i];

				// 1. Заголовок ошибки: [ИмяФайла](Строка, Символ): error [КодОшибки] [Слой]: Комментарий
				builder.AppendLine($"[{err.Pos.SourceFile}]({err.Pos.Line},{err.Pos.Symbol}): error {err.ErrorCode} [{err.Layer}]: {err.Comment}");

				// Проверяем наличие исходного файла и корректность номера строки
				if (parsedSources.TryGetValue(err.Pos.SourceFile, out var lines) &&
					err.Pos.Line > 0 && err.Pos.Line <= lines.Length)
				{
					string sourceLine = lines[err.Pos.Line - 1];

					// Подготавливаем строку: заменяем табуляцию на 4 пробела, чтобы указатель '^' не съезжал
					string printableLine = sourceLine.Replace("\t", "    ");

					// Форматируем номер строки с выравниванием
					string lineNumStr = err.Pos.Line.ToString();
					string margin = new string(' ', lineNumStr.Length);

					builder.AppendLine($"{margin} |");
					builder.AppendLine($"{lineNumStr} | {printableLine}");

					// Рассчитываем смещение указателя '^' с учетом замененных знаков табуляции
					int pointerOffset = CalculatePointerOffset(sourceLine, err.Pos.Symbol);
					string pointerPadding = new string(' ', pointerOffset);

					builder.AppendLine($"{margin} | {pointerPadding}^");
				}

				// Разделитель между ошибками
				if (i < _errors.Count - 1)
				{
					builder.AppendLine();
				}
			}

			return builder.ToString();
		}

		/// <summary>
		/// Корректно рассчитывает смещение указателя с учетом табуляций в исходной строке.
		/// </summary>
		private static int CalculatePointerOffset(string originalLine, int symbolPosition)
		{
			int offset = 0;
			// symbolPosition передается обычно от 1 или 0 (берем безопасный предел)
			int targetIndex = Math.Min(Math.Max(0, symbolPosition - 1), originalLine.Length);

			for (int i = 0; i < targetIndex; i++)
			{
				if (originalLine[i] == '\t')
					offset += 4; // Выравниваем под 4 пробела из printableLine
				else
					offset += 1;
			}

			return offset;
		}

	}
}
