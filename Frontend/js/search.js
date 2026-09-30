// ==========================================================================
// Tra Cứu Sách (Độc Giả) — tìm kiếm nâng cao
// API: GET /api/Book/search?keyword=&theloai=&page=&pageSize=  (Anonymous)
// ==========================================================================

document.addEventListener('DOMContentLoaded', () => {
    const searchForm = document.getElementById('searchForm');
    if (searchForm) {
        searchForm.addEventListener('submit', e => {
            e.preventDefault();
            traCuuSach();
        });
        napdsTheLoai();
        traCuuSach(); // hiển thị toàn bộ sách khi vào trang
    }
});

// Nạp dropdown Thể loại từ dữ liệu sách
async function napdsTheLoai() {
    try {
        const res = await window.api.get('/Book/search?page=1&pageSize=1000');
        if (!res.success || !res.data) return;
        const select = document.getElementById('searchTheloai');
        if (!select) return;

        [...new Set(res.data.map(b => b.theloai).filter(Boolean))]
            .sort()
            .forEach(tl => {
                const opt = document.createElement('option');
                opt.value = tl;
                opt.textContent = tl;
                select.appendChild(opt);
            });
    } catch (err) {
        console.error('Lỗi nạp thể loại:', err);
    }
}

// Tìm kiếm: keyword (tên / ISBN / tác giả) + bộ lọc thể loại
async function traCuuSach() {
    const box = document.getElementById('ketQuaTimKiem');
    if (!box) return;
    box.innerHTML = '<p class="text-muted">Đang tìm...</p>';

    const params = new URLSearchParams({ page: 1, pageSize: 50 });
    const keyword = (document.getElementById('searchInput')?.value || '').trim();
    const theloai = document.getElementById('searchTheloai')?.value || '';
    if (keyword) params.set('keyword', keyword);
    if (theloai) params.set('theloai', theloai);

    try {
        const res = await window.api.get(`/Book/search?${params.toString()}`);

        if (res.success && res.data && res.data.length > 0) {
            box.innerHTML = '<div class="d-flex" style="gap:20px;flex-wrap:wrap;">' +
                res.data.map(b => {
                    const badge = b.sobancaosangio > 0
                        ? `<span class="badge badge-success">Còn ${b.sobancaosangio} quyển</span>`
                        : `<span class="badge badge-danger">Đã hết</span>`;
                    return `
                    <div style="border:1px solid var(--border-color);border-radius:8px;padding:15px;width:220px;text-align:center;">
                        <div style="height:150px;background:#e2e8f0;border-radius:6px;margin-bottom:15px;display:flex;align-items:center;justify-content:center;">
                            <i class="fa-solid fa-book" style="font-size:3rem;color:#94a3b8;"></i>
                        </div>
                        <h4 style="margin-bottom:5px;">${b.title}</h4>
                        <p class="text-muted" style="font-size:0.85rem;margin-bottom:5px;">${b.tacgia || 'Không rõ tác giả'}</p>
                        <p class="text-muted" style="font-size:0.8rem;margin-bottom:8px;">${b.theloai || ''}${b.namxuatban ? ' · ' + b.namxuatban : ''}</p>
                        ${badge}
                    </div>`;
                }).join('') + '</div>' +
                `<p class="text-muted mt-2" style="font-size:0.85rem;">Tìm thấy ${res.totalItems ?? res.data.length} kết quả.</p>`;
        } else {
            box.innerHTML = '<p class="text-muted">Không tìm thấy sách nào phù hợp.</p>';
        }
    } catch (err) {
        box.innerHTML = `<p style="color:red">Lỗi tìm kiếm: ${err.message}</p>`;
    }
}
