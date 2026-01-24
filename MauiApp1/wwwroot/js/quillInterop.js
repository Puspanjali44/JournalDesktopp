window.quillEditors = {};

window.initQuill = (editorId, html) => {
    const toolbarOptions = [
        ['bold', 'italic', 'underline'],
        [{ 'header': 1 }, { 'header': 2 }],
        [{ 'list': 'ordered' }, { 'list': 'bullet' }],
        ['link'],
        ['clean']
    ];

    const quill = new Quill('#' + editorId, {
        theme: 'snow',
        modules: { toolbar: toolbarOptions }
    });

    if (html) {
        quill.root.innerHTML = html;
    }

    window.quillEditors[editorId] = quill;
};

window.getQuillHtml = (editorId) => {
    return window.quillEditors[editorId].root.innerHTML;
};
