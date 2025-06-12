let editor;

const languageMapping = {
    'python3': 'python',
    'c': 'c',
    'cpp': 'cpp',
    'csharp': 'csharp',
    'javascript': 'javascript'
};

const codeExamples = {
    python: `# Пример кода на Python
def greet(name):
    print('Hello, ' + name)

greet('World')`,
    c: `/* Пример кода на C */
#include <stdio.h>

int main() {
    printf("Hello, World\\n");
    return 0;
}`,
    cpp: `// Пример кода на C++
#include <iostream>

int main() {
    std::cout << "Hello, World" << std::endl;
    return 0;
}`,
    csharp: `// Пример кода на C#
using System;

class Program {
    static void Main() {
        Console.WriteLine("Hello, World");
    }
}`,
    javascript: `// Пример кода на JavaScript
function greet(name) {
    console.log('Hello, ' + name);
}
greet('World');`,
};

function initializeMonaco(language) {
    if (editor) {
        editor.dispose();
    }

    require.config({ paths: { 'vs': 'https://unpkg.com/monaco-editor/min/vs' } });

    require(['vs/editor/editor.main'], function () {
        editor = monaco.editor.create(document.getElementById('monaco-editor'), {
            language: language,
            automaticLayout: true,
            lineNumbers: 'on',
            theme: 'vs-dark',
            minimap: {
                enabled: true
            },
            suggestOnTriggerCharacters: true,
            quickSuggestions: {
                other: true,
                comments: true,
                strings: true
            }
        });

        if (codeExamples[language]) {
            editor.setValue(codeExamples[language]);
        } else {
            console.warn(`Пример кода для языка "${language}" не найден.`);
        }
    });
}

function getEditorValue() {
    if (editor) {
        return editor.getValue();
    } else {
        console.warn("Редактор еще не инициализирован.");
        return '';
    }
}

function disposeEditor() {
    if (editor) {
        editor.dispose();
        editor = null;
    }
}
