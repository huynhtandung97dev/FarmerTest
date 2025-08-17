// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.


let currentQ = "";
let currentPage = 1;
const input = document.getElementById("searchInput");
const clearBtn = document.getElementById("clearBtn");

let selectedIds = new Set(); // biến này lưu danh sách id khi check vào

const toast = (type, message, timeout = 4500) => {
    const title = { success: 'Thành công', error: 'Lỗi', warning: 'Cảnh báo', info: 'Thông báo' }[type] || 'Thông báo';
    const fn = (iziToast[type] || iziToast.info).bind(iziToast);
    fn({ title, message, position: 'bottomRight', timeout, progressBar: true, close: true });
};

input.addEventListener("input", function () {
    if (this.value.trim() !== "") {
        clearBtn.classList.remove("d-none"); // hiện nút
    } else {
        clearBtn.classList.add("d-none"); // ẩn nút
    }
});

function getAntiForgery() {
    const form = document.getElementById("__af");
    const input = form?.querySelector('input[name="__RequestVerificationToken"]');
    return input?.value ?? "";
}

async function loadTable(page = 1) {
    currentPage = page;
    const q = new URLSearchParams(window.location.search).get("q") || document.querySelector('input[name="q"]').value || "";
    currentQ = q;
    const url = `/Home/Table?page=${page}&q=${encodeURIComponent(q)}`;
    try {
        const html = await fetch(url, { cache: 'no-store' }).then(r => r.text());
        document.getElementById('table-container').innerHTML = html;
        wireTableSelection();
    } catch {
        toast('error', 'Không tải được danh sách.');
    }
    
}

function gotoPage(p) {
    if (p < 1) return false;
    loadTable(p);
    return false;
}

function submitSearch(e) {
    e.preventDefault();
    const q = new FormData(e.target).get('q') || '';
    const url = new URL(window.location);
    if (q) url.searchParams.set('q', q); else url.searchParams.delete('q');
    window.history.replaceState({}, '', url);
    loadTable(1);
    return false;
}

function clearSearch() {
    input.value = "";
    const url = new URL(window.location);
    url.searchParams.delete('q');
    window.history.replaceState({}, '', url);
    loadTable(1);
    clearBtn.classList.add("d-none");
    input.focus();
    toast('info', 'Đã xoá bộ lọc tìm kiếm', 2000);
}

async function openUpsert(id) {
    const url = id ? `/Home/CreateOrEdit/${id}` : `/Home/CreateOrEdit`;
    try {
        const html = await fetch(url).then(r => r.text());
        document.getElementById('modalUpsertBody').innerHTML = html;
        const modal = new bootstrap.Modal(document.getElementById('modalUpsert'));
        modal.show();
    } catch {
        toast('error', 'Không tải được biểu mẫu.');
    }
}

async function submitUpsert(e) {
    e.preventDefault();
    const form = e.target;
    const fd = new FormData(form);

    try {
        const resp = await fetch(form.action, {
            method: 'POST',
            body: fd,
            headers: { 'RequestVerificationToken': getAntiForgery() }
        });

        if (resp.ok) {
            // Thành công – đóng modal & reload bảng
            bootstrap.Modal.getInstance(document.getElementById('modalUpsert'))?.hide();
            await loadTable(currentPage);
            toast('success', 'Lưu dữ liệu thành công');
        } else if (resp.status === 400) {
            // Lỗi validate – server trả về html partial
            const html = await resp.text();
            document.getElementById('modalUpsertBody').innerHTML = html;
            toast('warning', 'Dữ liệu không hợp lệ. Kiểm tra các ô báo đỏ.');
            setTimeout(() => document.querySelector('#modalUpsertBody .input-validation-error')?.focus(), 0);

        } else {
            toast('error', 'Có lỗi xảy ra khi lưu dữ liệu.');
        }
    } catch {
        toast('error', 'Không thể kết nối máy chủ.');
    }
    
    return false;
}

async function confirmDelete(id) {
    if (!confirm('Xoá bản ghi này?')) return;
    const fd = new FormData();
    fd.append('__RequestVerificationToken', getAntiForgery());
    try {
        const resp = await fetch(`/Home/Delete/${id}`, { method: 'POST', body: fd });
        if (resp.ok) {
            await loadTable(currentPage);
            toast('success', 'Đã xoá bản ghi');
        }
        else toast('error', 'Xoá thất bại');
    } catch {
        toast('error', 'Không thể kết nối máy chủ.');
    }
    
}

function wireTableSelection() {
    selectedIds.clear();
    updateSelectedUI();

    const chkAll = document.getElementById('chkAll');
    const rowChecks = document.querySelectorAll('.row-check');

    if (chkAll) {
        chkAll.addEventListener('change', () => {
            rowChecks.forEach(cb => {
                cb.checked = chkAll.checked;
                if (cb.checked) selectedIds.add(cb.value); else selectedIds.delete(cb.value);
            });
            updateTriState();
            updateSelectedUI();
        });
        updateTriState();
        updateSelectedUI();
    }

    rowChecks.forEach(cb => {
        cb.addEventListener('change', () => {
            if (cb.checked) selectedIds.add(cb.value); else selectedIds.delete(cb.value);
            // cập nhật trạng thái chkAll
            const allChecked = Array.from(document.querySelectorAll('.row-check')).every(x => x.checked);
            if (chkAll) chkAll.checked = allChecked;
            updateTriState();
            updateSelectedUI();
        });
    });
}

function updateTriState() {
    const chkAll = document.getElementById('chkAll');
    const rowChecks = Array.from(document.querySelectorAll('.row-check'));
    const total = rowChecks.length;
    const checked = rowChecks.filter(cb => cb.checked).length;

    if (!chkAll) return;

    if (total === 0) {
        chkAll.checked = false;
        chkAll.indeterminate = false;
        chkAll.disabled = true;
        return;
    }

    chkAll.disabled = false;
    chkAll.indeterminate = checked > 0 && checked < total;
    chkAll.checked = checked === total;
}

function updateSelectedUI() {
    const btn = document.getElementById('btnDeleteSelected');
    if (!btn) return;
    const hasAny = selectedIds.size > 0;
    btn.classList.toggle('d-none', !hasAny);
}

async function deleteSelected() {
    if (selectedIds.size === 0) return;
    if (!confirm(`Xoá ${selectedIds.size} bản ghi đã chọn?`)) return;

    const fd = new FormData();
    fd.append('__RequestVerificationToken', getAntiForgery());
    for (const id of selectedIds) fd.append('ids', id);

    try {
        const resp = await fetch('/Home/DeleteMany', { method: 'POST', body: fd });
        if (resp.ok) {
            const n = selectedIds.size;
            selectedIds.clear();
            await loadTable(currentPage);
            toast?.('success', `Đã xoá ${n} bản ghi`);
        } else {
            const msg = await resp.text().catch(() => '');
            toast?.('error', msg || 'Xoá hàng loạt thất bại');
        }
    } catch {
        toast?.('error', 'Không thể kết nối máy chủ.');
    }
}