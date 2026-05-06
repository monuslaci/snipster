window.adjustTextAreaHeight = function (elementId) {
    var textarea = document.getElementById(elementId);
    if (textarea) {
        // Reset the height to auto so the height will adjust based on content
        textarea.style.height = 'auto';

        // Set the height to match the scroll height of the content
        var newHeight = textarea.scrollHeight;

        // Get the maximum height - use CSS max-height or fallback
        var maxHeight = 250; 

        // Use the smaller of the new height and the maximum height
        textarea.style.height = (newHeight > maxHeight ? maxHeight : newHeight) + 'px';
    }
};


function updateUrlWithoutQueryParam(url) {
    window.history.replaceState({}, document.title, url);
}

// Scroll functions
window.scrollToTop = function() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
};

window.scrollToBottom = function() {
    window.scrollTo({ top: document.body.scrollHeight, behavior: 'smooth' });
};

// Monaco Code Editor with Syntax Highlighting
window.monacoEditor = {
    editors: {},
    
    // Map language names to Monaco language IDs
    getMonacoLanguage: function(language) {
        var languageMap = {
            'javascript': 'javascript',
            'typescript': 'typescript',
            'python': 'python',
            'csharp': 'csharp',
            'java': 'java',
            'cpp': 'cpp',
            'c': 'c',
            'html': 'html',
            'css': 'css',
            'sql': 'sql',
            'json': 'json',
            'xml': 'xml',
            'bash': 'shell',
            'powershell': 'powershell',
            'ruby': 'ruby',
            'php': 'php',
            'go': 'go',
            'rust': 'rust',
            'swift': 'swift',
            'kotlin': 'kotlin',
            'yaml': 'yaml',
            'markdown': 'markdown',
            'razor': 'razor',
            'plaintext': 'plaintext'
        };
        return languageMap[language?.toLowerCase()] || 'plaintext';
    },
    
    // Initialize Monaco editor in a container
    createEditor: function(containerId, language, initialValue, isReadOnly) {
        var container = document.getElementById(containerId);
        if (!container) {
            console.error('Container not found:', containerId);
            return;
        }
        
        var self = this;
        var monacoLang = this.getMonacoLanguage(language);
        
        // Dispose existing editor if any
        if (this.editors[containerId]) {
            this.editors[containerId].dispose();
            delete this.editors[containerId];
        }
        
        // Clear container
        container.innerHTML = '';
        
        require(['vs/editor/editor.main'], function() {
            var editor = monaco.editor.create(container, {
                value: initialValue || '',
                language: monacoLang,
                theme: 'vs-dark',
                automaticLayout: true,
                minimap: { enabled: false },
                scrollBeyondLastLine: false,
                fontSize: 14,
                lineNumbers: 'on',
                wordWrap: 'off',
                readOnly: isReadOnly || false,
                renderLineHighlight: 'all',
                tabSize: 4,
                insertSpaces: true,
                scrollbar: {
                    vertical: 'auto',
                    horizontal: 'auto'
                }
            });
            
            self.editors[containerId] = editor;
            self.fitEditorHeight(containerId);

            editor.onDidContentSizeChange(function() {
                self.fitEditorHeight(containerId);
            });

            window.addEventListener('resize', function() {
                self.fitEditorHeight(containerId);
            });
        });
    },

    fitEditorHeight: function(containerId) {
        var container = document.getElementById(containerId);
        var editor = this.editors[containerId];

        if (!container || !editor) {
            return;
        }

        var panel = container.closest('.editor-panel');
        var codeGroup = container.closest('.code-editor-container');

        if (!panel || !codeGroup) {
            editor.layout();
            return;
        }

        container.style.height = '';
        editor.layout();
    },
    
    // Get the current value from an editor
    getValue: function(containerId) {
        var editor = this.editors[containerId];
        if (editor) {
            return editor.getValue();
        }
        return '';
    },
    
    // Set the value of an editor
    setValue: function(containerId, value) {
        var editor = this.editors[containerId];
        if (editor) {
            editor.setValue(value || '');
        }
    },
    
    // Update the language of an editor
    setLanguage: function(containerId, language) {
        var editor = this.editors[containerId];
        if (editor) {
            var model = editor.getModel();
            if (model) {
                monaco.editor.setModelLanguage(model, this.getMonacoLanguage(language));
            }
        }
    },
    
    // Set read-only state
    setReadOnly: function(containerId, isReadOnly) {
        var editor = this.editors[containerId];
        if (editor) {
            editor.updateOptions({ readOnly: isReadOnly });
        }
    },
    
    // Dispose an editor
    disposeEditor: function(containerId) {
        var editor = this.editors[containerId];
        if (editor) {
            editor.dispose();
            delete this.editors[containerId];
        }
    },
    
    // Dispose all editors
    disposeAll: function() {
        for (var id in this.editors) {
            if (this.editors.hasOwnProperty(id)) {
                this.editors[id].dispose();
            }
        }
        this.editors = {};
    }
};
