export function complete(token) {
    const form = document.createElement('form');
    form.method = 'post';
    form.action = '/integration/auth/result';
    const input = document.createElement('input');
    input.type = 'hidden';
    input.name = 'token';
    input.value = token;
    form.appendChild(input);
    document.body.appendChild(form);
    form.submit();
}
