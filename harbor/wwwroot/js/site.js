// Show chart values for a mouse, keyboard or touch selection.
document.querySelectorAll('[data-chart-detail]').forEach(segment => {
    const detail = segment.closest('.asset-chart').querySelector('.chart-detail');
    const show = () => detail.textContent = segment.dataset.chartDetail;
    segment.onmouseenter = show;
    segment.onfocus = show;
    segment.onclick = show;
});

// Help dialogs.
const dialog = document.getElementById('info-dialog');
const help = {
    how: ['How Harbor works', 'Upload PDFs, redact detected private details locally, and send processed files to AWS. Live financial extraction is not connected yet.'],
    security: ['Document processing', 'PDFs are redacted locally and uploaded to the team’s S3 bucket. Originals are deleted locally. Scans may still contain private details.'],
    help: ['Explore your workspace', 'Open a profile to see the financial overview, Documents or Reviews.']
};
document.querySelectorAll('[data-info]').forEach(button => {
    button.onclick = () => {
        const [title, text] = help[button.dataset.info];
        document.getElementById('info-title').textContent = title;
        document.getElementById('info-text').textContent = text;
        dialog.showModal();
        document.body.classList.add('dialog-open');
    };
});
if (dialog) {
    document.getElementById('close-info').onclick = () => dialog.close();
    dialog.onclose = () => document.body.classList.remove('dialog-open');
    dialog.onclick = event => {
        const box = dialog.getBoundingClientRect();
        if (event.clientX < box.left || event.clientX > box.right || event.clientY < box.top || event.clientY > box.bottom) dialog.close();
    };
}

// Switch inspector views and keep keyboard focus on the opener.
const panel = document.getElementById('picture-panel');
if (panel) {
    const inspector = bootstrap.Offcanvas.getOrCreateInstance(panel, { backdrop: false, scroll: true });
    const titles = { documents: 'Source documents', reviews: 'Needs review' };
    const tabs = document.querySelectorAll('.panel-tabs [data-panel], .picture-actions [data-panel]');
    let opener;
    function selectView(view) {
        tabs.forEach(tab => {
            const active = tab.dataset.panel === view;
            tab.classList.toggle('active', active);
            tab.setAttribute('aria-pressed', active);
            if (tab.hasAttribute('aria-controls')) tab.setAttribute('aria-expanded', active);
        });
    }
    document.querySelectorAll('[data-panel]').forEach(button => {
        button.onclick = () => {
            const view = button.dataset.panel;
            document.getElementById('panel-title').textContent = titles[view];
            panel.querySelectorAll('[data-panel-content]').forEach(section => section.hidden = section.dataset.panelContent !== view);
            selectView(view);
            if (!panel.classList.contains('show') && !panel.classList.contains('showing')) { opener = button; inspector.show(); }
            else if (button.closest('[data-panel-content]')) document.getElementById('panel-title').focus();
        };
    });
    document.getElementById('close-panel').onclick = () => inspector.hide();
    panel.addEventListener('hidden.bs.offcanvas', () => { selectView(null); opener?.focus(); });
}

// Add selections together; remove individual files without replacing the rest.
const input = document.getElementById('documents');
if (input) {
    const files = [];
    function showFiles() {
        const transfer = new DataTransfer();
        const list = document.getElementById('selected-files');
        list.replaceChildren();
        files.forEach((file, index) => {
            transfer.items.add(file);
            const row = document.getElementById('file-row').content.cloneNode(true);
            row.querySelector('span').textContent = file.name;
            const remove = row.querySelector('button');
            remove.setAttribute('aria-label', 'Remove ' + file.name);
            remove.onclick = () => { files.splice(index, 1); showFiles(); };
            list.append(row);
        });
        input.files = transfer.files;
        let error = '';
        if (files.some(file => !/\.pdf$/i.test(file.name))) error = 'Choose PDF files only.';
        else if (files.length > 10) error = 'Choose up to 10 PDFs.';
        else if (files.some(file => file.size > 10 * 1024 * 1024)) error = 'Use PDFs up to 10 MB each.';
        input.setCustomValidity(error);
        let status = 'No files selected';
        if (files.length) status = files.length + (files.length === 1 ? ' file selected' : ' files selected');
        document.getElementById('file-status').textContent = error || status;
    }
    input.onchange = () => {
        for (const file of input.files) {
            if (!files.some(saved => saved.name === file.name && saved.size === file.size && saved.lastModified === file.lastModified)) files.push(file);
        }
        showFiles();
    };
}
const deleteForm = document.querySelector('[data-delete-profile]');
if (deleteForm) deleteForm.onsubmit = () => confirm('Delete this local profile? Copies already uploaded to AWS will remain.');
