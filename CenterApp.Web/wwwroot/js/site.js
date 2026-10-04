var APP_COLOR = '#4b2fd0';

function appModal() {
    return bootstrap.Modal.getOrCreateInstance(document.getElementById('appModal'));
}

// SweetAlert شغال حتى والـ Modal مفتوح (بنقفل الـ focus trap بتاع Bootstrap مؤقتاً)
function swalShow(opts) {
    var el = document.getElementById('appModal');
    var trap = null;
    try {
        var inst = bootstrap.Modal.getInstance(el);
        if (inst && el.classList.contains('show') && inst._focustrap) { trap = inst._focustrap; trap.deactivate(); }
    } catch (e) { }

    return Swal.fire($.extend({
        confirmButtonText: 'حسناً',
        cancelButtonText: 'إلغاء',
        confirmButtonColor: APP_COLOR,
        heightAuto: false
    }, opts)).then(function (r) {
        try { if (trap && el.classList.contains('show')) trap.activate(); } catch (e) { }
        return r;
    });
}

// رسالة: icon = success | error | warning | info
function notify(icon, text, cb) {
    return swalShow({ icon: icon, title: text }).then(function () { if (cb) cb(); });
}

// تأكيد: بيرجّع Promise فيها true / false
function askConfirm(text, okText) {
    return swalShow({
        icon: 'warning', title: text, showCancelButton: true,
        confirmButtonText: okText || 'تأكيد', reverseButtons: true
    }).then(function (r) { return r.isConfirmed; });
}

// أي alert قديمة بتتحول لـ SweetAlert
if (window.Swal) window.alert = function (m) { notify('info', String(m)); };

function openForm(url, title, large) {
    $.get(url).done(function (html) {
        $('#appModalTitle').text(title || '');
        $('#appModalBody').html(html);
        $('#appModalDialog').toggleClass('modal-lg', !!large);
        appModal().show();
    }).fail(function (xhr) {
        if (xhr.status === 409) notify('warning', xhr.responseText);
        else notify('error', 'تعذر التحميل (' + xhr.status + ')');
    });
}

function handleResult(r, onOk) {
    if (r && r.ok) {
        if (onOk) { onOk(r); return; }
        appModal().hide();
        Swal.fire({ icon: 'success', title: r.message || 'تم الحفظ', timer: 1200, showConfirmButton: false, heightAuto: false })
            .then(function () { location.reload(); });
    } else {
        notify('error', (r && r.message) || 'حدث خطأ');
    }
}

function ajaxFail(xhr) {
    notify('error', 'حصل خطأ في السيرفر (' + xhr.status + ')');
}

function postJson(url, data, onOk) {
    $.ajax({ url: url, type: 'POST', data: data })
        .done(function (r) { handleResult(r, onOk); })
        .fail(ajaxFail);
}

function postBody(url, obj, onOk) {
    $.ajax({ url: url, type: 'POST', contentType: 'application/json', data: JSON.stringify(obj) })
        .done(function (r) { handleResult(r, onOk); })
        .fail(ajaxFail);
}

function submitForm(formSelector, url, onOk) {
    postJson(url, $(formSelector).serialize(), onOk);
}

function confirmPost(url, data, msg) {
    askConfirm(msg || 'تأكيد العملية؟', 'نعم').then(function (ok) {
        if (ok) postJson(url, data, function () { location.reload(); });
    });
}