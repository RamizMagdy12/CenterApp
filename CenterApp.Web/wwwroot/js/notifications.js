(function () {
    if (window.__bellStarted) return;
    window.__bellStarted = true;

    function scrollChat() {
        var el = document.getElementById('chatLog');
        if (el) el.scrollTop = el.scrollHeight;
    }

    window.appendChat = function (senderName, body, when, mine) {
        var log = document.getElementById('chatLog');
        if (!log) return;
        var empty = log.querySelector('.chat-empty');
        if (empty) empty.remove();
        var div = document.createElement('div');
        div.className = 'bubble ' + (mine ? 'mine' : 'theirs');
        var name = document.createElement('div');
        name.className = 'bubble-name';
        name.textContent = senderName || '';
        var text = document.createElement('div');
        text.className = 'bubble-body';
        text.textContent = body || '';
        var time = document.createElement('small');
        time.textContent = when || '';
        div.appendChild(name);
        div.appendChild(text);
        div.appendChild(time);
        log.appendChild(div);
        scrollChat();
    };

    function renderBell(data) {
        var list = document.getElementById('bellList');
        var badge = document.getElementById('bellBadge');
        if (!list || !badge) return;
        var n = (data && data.unread) || 0;
        if (n > 0) {
            badge.textContent = n > 9 ? '9+' : String(n);
            badge.hidden = false;
        } else {
            badge.hidden = true;
        }
        list.textContent = '';
        var items = (data && data.items) || [];
        if (!items.length) {
            var empty = document.createElement('div');
            empty.className = 'tb-pop-empty';
            empty.textContent = (window.L10n && window.L10n.noNotifications) || 'مفيش إشعارات جديدة';
            list.appendChild(empty);
            return;
        }
        items.forEach(function (it) {
            var a = document.createElement('a');
            a.className = 'tb-note' + (it.isRead ? '' : ' unread');
            a.href = it.url || '#';
            var title = document.createElement('b');
            title.textContent = it.title || '';
            var body = document.createElement('span');
            body.textContent = it.body || '';
            var when = document.createElement('small');
            when.textContent = it.when || '';
            a.appendChild(title);
            a.appendChild(body);
            a.appendChild(when);
            list.appendChild(a);
        });
    }

    function loadBell() {
        $.get('/Notifications/Recent').done(renderBell);
    }

    function startHub() {
        if (!window.signalR) return;
        var connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/app')
            .withAutomaticReconnect()
            .build();
        connection.on('incoming', function (m) {
            if (window.chatThreadId && Number(m.conversationId) === Number(window.chatThreadId)) {
                appendChat(m.senderName, m.body, m.sentAtText, false);
                $.post('/Notifications/Read', { conversationId: m.conversationId }).always(loadBell);
            } else {
                loadBell();
            }
        });
        connection.start().catch(function () { });
    }

    $(function () {
        if (!window.__tbPop) {
            $(document).on('click', '[data-pop]', function (e) {
                e.stopPropagation();
                var $p = $('#' + $(this).data('pop'));
                $('.tb-pop').not($p).removeClass('show');
                $p.toggleClass('show');
            });
            $(document).on('click', function () { $('.tb-pop').removeClass('show'); });
            $(document).on('click', '.tb-pop', function (e) { e.stopPropagation(); });
        }
        loadBell();
        scrollChat();
        $('#bellReadAll').on('click', function () {
            $.post('/Notifications/Read').always(loadBell);
        });
        $('#bellClear').on('click', function () {
            askConfirm((window.L10n && window.L10n.clearAllAsk) || 'مسح كل إشعاراتك؟', (window.L10n && window.L10n.clear) || 'مسح').then(function (ok) {
                if (!ok) return;
                $.post('/Notifications/Clear').done(function (r) {
                    if (r && r.ok === false) notify('error', r.message || ((window.L10n && window.L10n.error) || 'حصل خطأ'));
                }).always(loadBell);
            });
        });
        $('#chatDelete').on('click', function () {
            if (!window.chatDeleteUrl) return;
            askConfirm((window.L10n && window.L10n.clearChatAsk) || 'مسح الشات ده؟', (window.L10n && window.L10n.clear) || 'مسح').then(function (ok) {
                if (!ok) return;
                $.post(window.chatDeleteUrl, { id: window.chatThreadId }).done(function (r) {
                    if (r && r.ok) location.href = window.chatAfterDelete || '/Messages';
                    else notify('error', (r && r.message) || ((window.L10n && window.L10n.error) || 'حصل خطأ'));
                }).fail(function (xhr) { if (window.ajaxFail) ajaxFail(xhr); });
            });
        });
        $('#chatForm').on('submit', function (e) {
            e.preventDefault();
            var $body = $('#chatBody');
            var text = $.trim($body.val());
            if (!text || !window.chatSendUrl) return;
            var data = { body: text };
            if (window.chatTeacherId) data.teacherId = window.chatTeacherId;
            var $btn = $(this).find('button[type=submit]').prop('disabled', true);
            $.post(window.chatSendUrl, data).done(function (r) {
                if (!r || !r.ok) {
                    notify('error', (r && r.message) || ((window.L10n && window.L10n.error) || 'حصل خطأ'));
                    return;
                }
                if (r.id) window.chatThreadId = r.id;
                $body.val('');
                appendChat(window.chatMyName || ((window.L10n && window.L10n.you) || 'أنت'), text, r.extra || '', true);
            }).fail(function (xhr) {
                if (window.ajaxFail) ajaxFail(xhr);
            }).always(function () { $btn.prop('disabled', false); });
        });
        $('#chatBody').on('keydown', function (e) {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                $('#chatForm').trigger('submit');
            }
        });
        startHub();
    });
})();
