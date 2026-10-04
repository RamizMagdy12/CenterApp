(function () {
    var mobile = function () { return window.matchMedia('(max-width: 991.98px)').matches; };

    // ── فتح/غلق السايدبار
    try { if (!mobile() && localStorage.getItem('sbCollapsed') === '1') document.body.classList.add('sb-collapsed'); } catch (e) { }

    $('#sbToggle').on('click', function () {
        if (mobile()) {
            document.body.classList.toggle('sb-open');
        } else {
            var c = document.body.classList.toggle('sb-collapsed');
            try { localStorage.setItem('sbCollapsed', c ? '1' : '0'); } catch (e) { }
        }
    });
    $('#sbOverlay').on('click', function () { document.body.classList.remove('sb-open'); });

    // ── فتح/طي المجموعات
    $(document).on('click', '.mg-head', function () { $(this).closest('.mg').toggleClass('open'); });
    $('#sbCollapseAll').on('click', function () { $('.mg').removeClass('open'); });

    // ── بحث في الشاشات
    $('#sbSearch').on('input', function () {
        var q = $.trim($(this).val()).toLowerCase();
        $('.mg').each(function () {
            var $g = $(this), any = false;
            var titleHit = $g.find('.mg-title').text().toLowerCase().indexOf(q) > -1;
            $g.find('.mi').each(function () {
                var hit = !q || titleHit || $(this).text().toLowerCase().indexOf(q) > -1;
                $(this).toggle(hit);
                if (hit && q) any = true;
            });
            $g.toggle(!q || any);
            if (q && any) $g.addClass('open');
        });
    });

    // ── قوائم الـ Topbar
    $(document).on('click', '[data-pop]', function (e) {
        e.stopPropagation();
        var $p = $('#' + $(this).data('pop'));
        $('.tb-pop').not($p).removeClass('show');
        $p.toggleClass('show');
    });
    $(document).on('click', function () { $('.tb-pop').removeClass('show'); });
    $(document).on('click', '.tb-pop', function (e) { e.stopPropagation(); });
})();