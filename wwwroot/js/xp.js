// ============================================================
// KuruşMatik - Ana JavaScript Dosyası
// Burada taskbar saatini, bakiye güncellemesini, Start menüsünü
// ve AI Coach mantığını yazdım. Geliştirici 1'in endpoint'lerine
// AJAX ile bağlanıyorum.
// ============================================================

// --- TASKBAR SAATİ ---
// Her saniye güncellensin, gerçek Windows gibi görünsün
function updateClock() {
    const el = document.getElementById('taskbar-time');
    if (!el) return;
    const now = new Date();
    const h = now.getHours().toString().padStart(2, '0');
    const m = now.getMinutes().toString().padStart(2, '0');
    el.textContent = h + ':' + m;
}
setInterval(updateClock, 1000);
updateClock();

// --- BAKIYE AJAX GÜNCELLEMESİ ---
// Geliştirici 1'in /Transaction/GetCurrentBalance endpoint'ini
// her 30 saniyede bir çağırarak anlık bakiyeyi güncelliyorum.
// Sayfa yenilenmeden çalışıyor bu kısım.
function refreshBalance() {
    const el = document.getElementById('taskbar-balance-amount');
    if (!el) return;

    fetch('/Transaction/GetCurrentBalance')
        .then(r => r.ok ? r.json() : null)
        .then(data => {
            if (!data) return;
            el.textContent = formatMoney(data.balance) + ' ₺';
            el.className = data.balance >= 0 ? '' : 'negative';
        })
        .catch(() => { /* sessizce geç, kritik değil */ });
}

// Bakiyeyi 30 saniyede bir güncelle
setInterval(refreshBalance, 30000);
// Sayfa açıldığında da bir kere çalıştır
window.addEventListener('DOMContentLoaded', refreshBalance);

// --- PARA FORMATI YARDIMCI ---
function formatMoney(amount) {
    return new Intl.NumberFormat('tr-TR', {
        minimumFractionDigits: 2,
        maximumFractionDigits: 2
    }).format(amount);
}

// --- START MENÜSÜ ---
// Windows XP'deki sol-alt Start butonunun menüsünü yönetiyorum
document.addEventListener('DOMContentLoaded', function () {
    const startBtn = document.getElementById('xp-start-btn');
    const startMenu = document.getElementById('xp-start-menu');

    if (startBtn && startMenu) {
        startBtn.addEventListener('click', function (e) {
            e.stopPropagation();
            startMenu.classList.toggle('open');
        });

        // Başka yere tıklanırsa menü kapansın
        document.addEventListener('click', function () {
            startMenu.classList.remove('open');
        });
    }
});

// --- TOASTR BENZERİ BİLDİRİM SİSTEMİ ---
// Dışarıdan kütüphane yüklemek yerine kendim yazdım,
// ama Toastr ile aynı mantık. XP uyarı kutusu gibi görünecek.
window.xpNotify = {
    container: null,

    init() {
        if (!this.container) {
            this.container = document.createElement('div');
            this.container.id = 'xp-notify-container';
            this.container.style.cssText = `
                position: fixed; top: 8px; right: 8px; z-index: 99999;
                display: flex; flex-direction: column; gap: 4px;
                max-width: 320px;
            `;
            document.body.appendChild(this.container);
        }
    },

    show(message, type = 'info', duration = 3500) {
        this.init();

        // XP'nin o klasik uyarı ikonları
        const icons = { success: '✅', error: '❌', warning: '⚠️', info: 'ℹ️' };
        const colors = {
            success: { bg: '#EFFFEF', border: '#6AAA6A', title: 'KuruşMatik' },
            error:   { bg: '#FFF0F0', border: '#CC2020', title: 'Hata' },
            warning: { bg: '#FFFAEE', border: '#CCAA44', title: 'Uyarı' },
            info:    { bg: '#EFF5FF', border: '#7F9DB9', title: 'Bilgi' }
        };
        const c = colors[type] || colors.info;

        const box = document.createElement('div');
        box.style.cssText = `
            background: ${c.bg};
            border: 1px solid ${c.border};
            box-shadow: 3px 3px 6px rgba(0,0,0,0.4);
            padding: 0;
            font-family: Tahoma, sans-serif;
            font-size: 11px;
            animation: xpSlideIn 0.2s ease;
            overflow: hidden;
        `;

        box.innerHTML = `
            <div style="background: linear-gradient(to bottom, #4A9EE8, #0558A4); color:white;
                         padding: 3px 8px; display:flex; align-items:center; gap:6px; font-weight:bold;">
                <span style="font-size:12px;">${icons[type]}</span>
                <span style="flex:1;">${c.title}</span>
                <span style="cursor:pointer; font-size:14px; line-height:1;"
                      onclick="this.closest('[data-xp-notify]').remove()">×</span>
            </div>
            <div style="padding: 8px 10px;">${message}</div>
        `;
        box.setAttribute('data-xp-notify', '1');

        // CSS animasyon için stil ekle
        if (!document.getElementById('xp-notify-style')) {
            const s = document.createElement('style');
            s.id = 'xp-notify-style';
            s.textContent = `@keyframes xpSlideIn { from { opacity:0; transform:translateX(20px); } to { opacity:1; transform:translateX(0); } }`;
            document.head.appendChild(s);
        }

        this.container.appendChild(box);

        setTimeout(() => {
            box.style.opacity = '0';
            box.style.transition = 'opacity 0.3s';
            setTimeout(() => box.remove(), 300);
        }, duration);
    },

    success(msg) { this.show(msg, 'success'); },
    error(msg)   { this.show(msg, 'error', 5000); },
    warning(msg) { this.show(msg, 'warning', 4000); },
    info(msg)    { this.show(msg, 'info'); }
};

// TempData mesajları varsa bildirim olarak göster
// (Razor view'lardan window.xpMessages değişkeni set edilecek)
window.addEventListener('DOMContentLoaded', function () {
    if (window.xpMessages) {
        if (window.xpMessages.success) xpNotify.success(window.xpMessages.success);
        if (window.xpMessages.error)   xpNotify.error(window.xpMessages.error);
        if (window.xpMessages.warning) xpNotify.warning(window.xpMessages.warning);
    }
});

// --- AJAX HIZLI İŞLEM EKLEME ---
// Dashboard'daki hızlı form için, sayfa yenilenmeden POST atıyorum
window.quickAddTransaction = async function (e) {
    e.preventDefault();
    const form = document.getElementById('quick-transaction-form');
    if (!form) return;

    // Basit validasyon: tutar negatif olamaz
    const amountInput = form.querySelector('[name="Amount"]');
    const amount = parseFloat(amountInput.value);

    if (isNaN(amount) || amount <= 0) {
        xpNotify.error('Tutar 0\'dan büyük olmalıdır! Negatif değer girilemez.');
        amountInput.style.border = '2px solid red';
        amountInput.focus();
        return;
    }
    amountInput.style.border = '';

    // Gönder butonu devre dışı bırak
    const btn = form.querySelector('[type="submit"]');
    const originalText = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '⏳ Kaydediliyor...';

    try {
        // Anti-forgery token'ı formdan alıyorum
        const token = form.querySelector('[name="__RequestVerificationToken"]').value;
        const data = new FormData(form);

        const resp = await fetch('/Transaction/Create', {
            method: 'POST',
            headers: { 'RequestVerificationToken': token },
            body: data
        });

        if (resp.ok || resp.redirected) {
            xpNotify.success('İşlem başarıyla eklendi! 💰');
            form.reset();
            // Bakiyeyi hemen güncelle
            setTimeout(refreshBalance, 500);
            // İşlem tablosunu da yenile
            if (typeof refreshTransactionTable === 'function') refreshTransactionTable();
        } else {
            xpNotify.error('İşlem kaydedilirken hata oluştu.');
        }
    } catch (err) {
        xpNotify.error('Bağlantı hatası oluştu.');
    } finally {
        btn.disabled = false;
        btn.innerHTML = originalText;
    }
};

// --- İŞLEM SİL (AJAX) ---
window.deleteTransaction = async function (id, btn) {
    // Silme onayı - XP tarzı confirm (ileride SweetAlert2 eklenebilir)
    if (!confirm('Bu işlemi silmek istediğinizden emin misiniz?')) return;

    const row = btn.closest('tr');
    const token = document.querySelector('input[name="__RequestVerificationToken"]');

    try {
        const resp = await fetch('/Transaction/Delete/' + id, {
            method: 'POST',
            headers: {
                'X-Requested-With': 'XMLHttpRequest',
                'RequestVerificationToken': token ? token.value : ''
            }
        });

        const result = await resp.json();
        if (result.success) {
            row.style.backgroundColor = '#FFD4D4';
            setTimeout(() => { row.remove(); refreshBalance(); }, 400);
            xpNotify.success('İşlem silindi.');
        } else {
            xpNotify.error('Silme işlemi başarısız.');
        }
    } catch {
        xpNotify.error('Bir hata oluştu.');
    }
};

// --- AI COACH SİMÜLASYONU ---
// Gerçek bir API çağrısı yerine, kullanıcının harcama verilerini
// analiz edip yerel bir mantıkla tavsiye üretiyorum.
// Hoca sunumda internet erişimi olmasa bile çalışsın diye böyle yaptım.
window.loadAiCoach = function (totalIncome, totalExpense, categoryData) {
    const el = document.getElementById('ai-coach-content');
    const loadingEl = document.getElementById('ai-coach-loading');
    if (!el) return;

    // Yüklenme animasyonu göster
    if (loadingEl) loadingEl.style.display = 'block';
    el.style.display = 'none';

    // Gerçek bir API çağrısı gibi küçük bir gecikme
    setTimeout(() => {
        const advice = generateFinancialAdvice(totalIncome, totalExpense, categoryData);
        el.innerHTML = advice;
        if (loadingEl) loadingEl.style.display = 'none';
        el.style.display = 'block';
        // Efekt için typewriter animasyonu
        typeWriter(el, advice, 0);
    }, 1200);
};

// Harcama verisine göre tavsiye üret
function generateFinancialAdvice(income, expense, categories) {
    const balance = income - expense;
    const savingsRate = income > 0 ? ((balance / income) * 100).toFixed(1) : 0;
    const tips = [];

    // Tasarruf oranına göre genel yorum
    if (income === 0) {
        tips.push(`📊 Bu ay henüz gelir girişi yapılmamış. Gelirlerinizi ekleyerek daha doğru analiz yapabilirim.`);
    } else if (savingsRate < 0) {
        tips.push(`🚨 <strong>Dikkat!</strong> Bu ay gelirinizin <strong>${Math.abs(savingsRate)}%</strong> fazlasını harcadınız. Harcamaları kısıtlamak için bazı kategorileri gözden geçirin.`);
    } else if (savingsRate < 10) {
        tips.push(`⚠️ Tasarruf oranınız <strong>%${savingsRate}</strong> — bu oldukça düşük. Finansal uzmanlar en az %20 tasarruf önerir.`);
    } else if (savingsRate < 20) {
        tips.push(`👍 Tasarruf oranınız <strong>%${savingsRate}</strong>. İyi bir başlangıç, biraz daha artırabilirsiniz.`);
    } else {
        tips.push(`🏆 Harika! Gelirinizin <strong>%${savingsRate}</strong>'ini biriktiriyorsunuz. Finansal hedeflerinize yaklaşıyorsunuz!`);
    }

    // En yüksek harcama kategorisi varsa uyar
    if (categories && categories.length > 0) {
        const topCat = categories.reduce((a, b) => a.totalAmount > b.totalAmount ? a : b);
        const pct = income > 0 ? ((topCat.totalAmount / income) * 100).toFixed(0) : 0;
        tips.push(`💡 En yüksek harcamanız <strong>${topCat.categoryName}</strong> kategorisinde (${formatMoney(topCat.totalAmount)} ₺ — gelirinizin %${pct}'i).`);
    }

    // Genel finans tavsiyesi
    const generalTips = [
        '📌 50/30/20 kuralını deneyin: gelirinizin %50\'si ihtiyaçlar, %30\'u istekler, %20\'si tasarruf.',
        '📌 Küçük harcamaların toplamı çoğu zaman büyük faturalardan fazla olur — kahve, abonelikler gibi.',
        '📌 Beklenmedik giderler için gelirin %3\'ü kadar acil fon oluşturmanı öneririm.',
    ];
    tips.push(generalTips[Math.floor(Math.random() * generalTips.length)]);

    return tips.join('<br><br>');
}

// Yazı makinesi efekti (sadece görsel amaçlı)
function typeWriter(el, html, i) {
    // HTML tagları için hızlıca geç, sadece metin karakterlerini göster
    el.innerHTML = html.substring(0, i);
    if (i < html.length) {
        const delay = html[i] === '<' ? 0 : (html[i] === '.' || html[i] === '!' ? 40 : 8);
        setTimeout(() => typeWriter(el, html, i + (html[i] === '<' ? html.indexOf('>', i) - i + 1 : 1)), delay);
    }
}
