// Simple voucher UI helpers (in case of reuse elsewhere)
(function(){
  window.applyVoucher = async function(code){
    const formData = new FormData();
    formData.append('code', code);
    const res = await fetch('/Cart/ApplyVoucher', { method: 'POST', body: formData });
    return await res.json();
  };
  window.removeVoucher = async function(){
    const res = await fetch('/Cart/RemoveVoucher', { method: 'POST' });
    return await res.json();
  };
})();
