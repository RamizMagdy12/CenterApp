var APP_COLOR = '#4b2fd0';

function t(key, fallback) {
    var v = window.L10n && window.L10n[key];
    return v || fallback;
}

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
        confirmButtonText: t('ok', 'حسناً'),
        cancelButtonText: t('cancel', 'إلغاء'),
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
        confirmButtonText: okText || t('confirm', 'تأكيد'), reverseButtons: true
    }).then(function (r) { return r.isConfirmed; });
}

// أي alert قديمة بتتحول لـ SweetAlert
if (window.Swal) window.alert = function (m) { notify('info', String(m)); };

function sessionReplaced(xhr) {
    if (xhr.status !== 401) return false;
    location.href = '/Account/Login?replaced=1';
    return true;
}

function fitTables(root) {
    $(root || document).find('table').each(function () {
        var $t = $(this);
        if ($t.parent().hasClass('table-responsive')) return;
        $t.wrap('<div class="table-responsive"></div>');
    });
}

$(function () { fitTables(document); });

function openForm(url, title, large) {
    $.get(url).done(function (html) {
        $('#appModalTitle').text(title || '');
        $('#appModalBody').html(html);
        fitTables(document.getElementById('appModalBody'));
        $('#appModalDialog').toggleClass('modal-lg', !!large);
        appModal().show();
    }).fail(function (xhr) {
        if (sessionReplaced(xhr)) return;
        if (xhr.status === 409) notify('warning', xhr.responseText);
        else notify('error', t('loadFailed', 'تعذر التحميل ({0})').replace('{0}', xhr.status));
    });
}

function handleResult(r, onOk) {
    if (r && r.ok) {
        if (onOk) { onOk(r); return; }
        appModal().hide();
        Swal.fire({ icon: 'success', title: r.message || t('saved', 'تم الحفظ'), timer: 1200, showConfirmButton: false, heightAuto: false })
            .then(function () { location.reload(); });
    } else {
        notify('error', (r && r.message) || t('error', 'حدث خطأ'));
    }
}

function ajaxFail(xhr) {
    if (sessionReplaced(xhr)) return;
    notify('error', t('serverError', 'حصل خطأ في السيرفر ({0})').replace('{0}', xhr.status));
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
    askConfirm(msg || t('confirmAction', 'تأكيد العملية؟'), t('yes', 'نعم')).then(function (ok) {
        if (ok) postJson(url, data, function () { location.reload(); });
    });
}