function appModal() {
    return bootstrap.Modal.getOrCreateInstance(document.getElementById('appModal'));
}

function openForm(url, title, large) {
    $.get(url).done(function (html) {
        $('#appModalTitle').text(title || '');
        $('#appModalBody').html(html);
        $('#appModalDialog').toggleClass('modal-lg', !!large);
        appModal().show();
    }).fail(function (xhr) { alert('تعذر التحميل: ' + xhr.status); });
}

function handleResult(r, onOk) {
    if (r && r.ok) {
        if (onOk) onOk(r);
        else { appModal().hide(); location.reload(); }
    } else {
        alert((r && r.message) || 'حدث خطأ');
    }
}

function postJson(url, data, onOk) {
    $.ajax({ url: url, type: 'POST', data: data })
        .done(function (r) { handleResult(r, onOk); })
        .fail(function (xhr) { alert('خطأ: ' + xhr.status + ' ' + (xhr.responseText || '').substring(0, 200)); });
}

function postBody(url, obj, onOk) {
    $.ajax({ url: url, type: 'POST', contentType: 'application/json', data: JSON.stringify(obj) })
        .done(function (r) { handleResult(r, onOk); })
        .fail(function (xhr) { alert('خطأ: ' + xhr.status + ' ' + (xhr.responseText || '').substring(0, 200)); });
}

function submitForm(formSelector, url, onOk) {
    postJson(url, $(formSelector).serialize(), onOk);
}

function confirmPost(url, data, msg) {
    if (!confirm(msg || 'تأكيد العملية؟')) return;
    postJson(url, data, function () { location.reload(); });
}