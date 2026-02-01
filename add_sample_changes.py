#!/usr/bin/env python3
import re

# Read LocalizeMpqCommandHandler.cs
with open('dbc-localizer/LocalizeMpqCommandHandler.cs', 'r', encoding='utf-8') as f:
    content = f.read()

# Find and replace 1: Add sampleChanges dict initialization
pattern1 = r'(int localeTablesMerged = 0;)\s*\n\s*(foreach \(var dbcRelPath in selectedDbcs\))'
replacement1 = r'''\1
				var sampleChanges = new Dictionary<string, List<SampleChange>>();

				\2'''

content = re.sub(pattern1, replacement1, content)

# Find and replace 2: Update LocalizeDbc call
pattern2 = r'(out var stats\);)'
replacement2 = r'''\1.Replace("out var stats);", 
						"out var stats,\n\t\t\t\t\t\t\"enUS\",\n\t\t\t\t\t\ttrue,\n\t\t\t\t\t\tsampleChanges);")'''

# This is tricky with regex, let's do it manually
lines = content.split('\n')
new_lines = []
for i, line in enumerate(lines):
    if 'out var stats);' in line and 'LocalizeDbc' in '\n'.join(lines[max(0, i-15):i]):
        # This is the end of LocalizeDbc call
        new_lines.append(line.replace('out var stats);', 'out var stats,'))
        new_lines.append('\t\t\t\t\t\t"enUS",')
        new_lines.append('\t\t\t\t\t\ttrue,')
        new_lines.append('\t\t\t\t\t\tsampleChanges);')
    else:
        new_lines.append(line)

content = '\n'.join(new_lines)

# Find and replace 3: Add SampleChanges to perLocaleResults
pattern3 = r'(FieldsUpdated = localeFieldsUpdated\s*\n\s*\}\);)'
replacement3 = r'''\1.Replace("FieldsUpdated = localeFieldsUpdated", 
\t\t\t\tFieldsUpdated = localeFieldsUpdated,
\t\t\t\tSampleChanges = sampleChanges")'''

content = re.sub(pattern3, replacement3, content, flags=re.MULTILINE)

# Write back
with open('dbc-localizer/LocalizeMpqCommandHandler.cs', 'w', encoding='utf-8') as f:
    f.write(content)

print("Modified LocalizeMpqCommandHandler.cs")
