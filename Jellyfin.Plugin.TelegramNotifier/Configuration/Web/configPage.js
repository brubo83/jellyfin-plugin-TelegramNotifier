export default function (view) {
    const pluginUniqueId = 'fd76211d-17e0-4a72-a23f-c6eeb1e48b3a';
    const $ = (sel) => view.querySelector(sel);

    const state = {
        config: null,
        catalog: [],   // [{ key, group, label, scope: 'user' | 'server', defaultMessage }]
        users: [],     // [{ id, name }]
        messages: {},  // key -> template text currently shown/edited
        currentTemplateKey: null
    };

    const normalizeId = (id) => (id || '').replace(/-/g, '').toLowerCase();

    // ---------- Matrix ----------

    // Every checkbox has data-key and data-col ('server' or a user id).
    const allBoxes = () => Array.from(view.querySelectorAll('#Matrix input[type=checkbox]'));

    // Check all if at least one is unchecked, otherwise uncheck all.
    function toggleBoxes(boxes) {
        if (boxes.length === 0) return;
        const target = boxes.some((b) => !b.checked);
        boxes.forEach((b) => { b.checked = target; });
    }

    function cell(content, className) {
        const td = document.createElement('td');
        if (className) td.className = className;
        if (content instanceof Node) td.appendChild(content); else td.textContent = content;
        return td;
    }

    function checkbox(key, col, checked, title) {
        const input = document.createElement('input');
        input.type = 'checkbox';
        input.dataset.key = key;
        input.dataset.col = col;
        input.checked = checked;
        input.title = title;
        return input;
    }

    function renderMatrix() {
        const table = $('#Matrix');
        table.innerHTML = '';
        const rules = new Map((state.config.Rules || []).map((r) => [r.Key, r]));

        // Header: Notifica | Server | user1 | user2 ...
        const thead = document.createElement('thead');
        const headRow = document.createElement('tr');
        const corner = document.createElement('th');
        corner.textContent = 'Notifica';
        corner.className = 'tn-toggle';
        corner.title = 'Attiva/disattiva tutto';
        corner.addEventListener('click', () => toggleBoxes(allBoxes()));
        headRow.appendChild(corner);

        const columns = [{ id: 'server', name: 'Server' }].concat(state.users);
        for (const col of columns) {
            const th = document.createElement('th');
            th.textContent = col.name;
            th.className = 'tn-toggle' + (col.id === 'server' ? ' tn-serverhead' : '');
            th.title = col.id === 'server' ? 'Attiva/disattiva tutte le notifiche del server' : 'Attiva/disattiva tutte le notifiche di ' + col.name;
            th.addEventListener('click', () => toggleBoxes(allBoxes().filter((b) => b.dataset.col === col.id)));
            headRow.appendChild(th);
        }
        thead.appendChild(headRow);
        table.appendChild(thead);

        const tbody = document.createElement('tbody');
        let lastGroup = null;
        for (const def of state.catalog) {
            if (def.group !== lastGroup) {
                lastGroup = def.group;
                const groupRow = document.createElement('tr');
                groupRow.className = 'tn-group';
                const groupName = def.group;
                const label = document.createElement('span');
                label.textContent = groupName;
                label.className = 'tn-toggle';
                label.title = 'Attiva/disattiva tutto il gruppo';
                label.addEventListener('click', () => {
                    const keys = new Set(state.catalog.filter((d) => d.group === groupName).map((d) => d.key));
                    toggleBoxes(allBoxes().filter((b) => keys.has(b.dataset.key)));
                });
                groupRow.appendChild(cell(label));
                const filler = cell('');
                filler.colSpan = columns.length;
                groupRow.appendChild(filler);
                tbody.appendChild(groupRow);
            }

            const rule = rules.get(def.key) || { Enabled: false, UserIds: [] };
            const enabledUsers = new Set((rule.UserIds || []).map(normalizeId));
            const tr = document.createElement('tr');

            const name = document.createElement('span');
            name.textContent = def.label;
            name.className = 'tn-toggle';
            name.title = 'Attiva/disattiva per tutti';
            name.addEventListener('click', () => toggleBoxes(allBoxes().filter((b) => b.dataset.key === def.key)));
            tr.appendChild(cell(name));

            if (def.scope === 'server') {
                tr.appendChild(cell(checkbox(def.key, 'server', rule.Enabled === true, def.label)));
                state.users.forEach(() => tr.appendChild(cell('–', 'tn-na')));
            } else {
                tr.appendChild(cell('–', 'tn-na'));
                for (const user of state.users) {
                    tr.appendChild(cell(checkbox(def.key, user.id, enabledUsers.has(user.id), def.label + ' – ' + user.name)));
                }
            }
            tbody.appendChild(tr);
        }
        table.appendChild(tbody);
    }

    // ---------- Message templates ----------

    function storeCurrentTemplate() {
        if (state.currentTemplateKey) {
            state.messages[state.currentTemplateKey] = $('#TemplateText').value;
        }
    }

    function showTemplate(key) {
        storeCurrentTemplate();
        state.currentTemplateKey = key;
        $('#TemplateText').value = state.messages[key] || '';
    }

    function renderTemplates() {
        const select = $('#TemplateKey');
        select.innerHTML = '';
        const rules = new Map((state.config.Rules || []).map((r) => [r.Key, r]));
        let lastGroup = null;
        let optgroup = null;
        for (const def of state.catalog) {
            if (def.group !== lastGroup) {
                lastGroup = def.group;
                optgroup = document.createElement('optgroup');
                optgroup.label = def.group;
                select.appendChild(optgroup);
            }
            const option = document.createElement('option');
            option.value = def.key;
            option.textContent = def.label;
            optgroup.appendChild(option);
            const custom = rules.get(def.key)?.Message;
            state.messages[def.key] = custom && custom.trim() !== '' ? custom : def.defaultMessage;
        }
        state.currentTemplateKey = null;
        if (state.catalog.length > 0) {
            select.value = state.catalog[0].key;
            showTemplate(state.catalog[0].key);
        }
    }

    // ---------- Load / save ----------

    async function load() {
        Dashboard.showLoadingMsg();
        try {
            const [config, catalog, users] = await Promise.all([
                ApiClient.getPluginConfiguration(pluginUniqueId),
                ApiClient.getJSON(ApiClient.getUrl('TelegramNotifierApi/Catalog')),
                ApiClient.getUsers()
            ]);
            state.config = config;
            state.catalog = catalog;
            state.users = users
                .map((u) => ({ id: normalizeId(u.Id), name: u.Name }))
                .sort((a, b) => a.name.localeCompare(b.name));

            $('#EnablePlugin').checked = config.EnablePlugin;
            $('#BotToken').value = config.BotToken || '';
            $('#ChatId').value = config.ChatId || '';
            $('#ThreadId').value = config.ThreadId || '';
            $('#SilentNotification').checked = config.SilentNotification;
            $('#KeepSerieImage').checked = config.KeepSerieImage;
            $('#ServerDisplayUrl').value = config.ServerDisplayUrl || '';
            $('#ServerUrl').value = config.ServerUrl || '';
            $('#PlaybackProgressIntervalMinutes').value = config.PlaybackProgressIntervalMinutes || 15;
            $('#EmptyPlaceholderReplacement').value = config.EmptyPlaceholderReplacement ?? '...';
            $('#ProxyEnabled').checked = config.ProxyEnabled;
            $('#ProxyHost').value = config.ProxyHost || '';
            $('#ProxyPort').value = config.ProxyPort || '';
            $('#ProxyUsername').value = config.ProxyUsername || '';
            $('#ProxyPassword').value = config.ProxyPassword || '';

            // Opaque background for the sticky first column/header, matching the current theme.
            let bg = getComputedStyle(document.body).backgroundColor;
            if (!bg || bg === 'rgba(0, 0, 0, 0)' || bg === 'transparent') bg = '#202020';
            view.style.setProperty('--tn-bg', bg);

            renderMatrix();
            renderTemplates();
        } catch (err) {
            console.error('TelegramNotifier: failed to load configuration', err);
            Dashboard.alert('Impossibile caricare la configurazione di Telegram Notifier.');
        } finally {
            Dashboard.hideLoadingMsg();
        }
    }

    async function save(e) {
        e.preventDefault();
        storeCurrentTemplate();
        Dashboard.showLoadingMsg();
        try {
            const config = await ApiClient.getPluginConfiguration(pluginUniqueId);
            config.EnablePlugin = $('#EnablePlugin').checked;
            config.BotToken = $('#BotToken').value.trim();
            config.ChatId = $('#ChatId').value.trim();
            config.ThreadId = $('#ThreadId').value.trim();
            config.SilentNotification = $('#SilentNotification').checked;
            config.KeepSerieImage = $('#KeepSerieImage').checked;
            config.ServerDisplayUrl = $('#ServerDisplayUrl').value.trim();
            config.ServerUrl = $('#ServerUrl').value.trim();
            config.PlaybackProgressIntervalMinutes = parseInt($('#PlaybackProgressIntervalMinutes').value, 10) || 15;
            config.EmptyPlaceholderReplacement = $('#EmptyPlaceholderReplacement').value;
            config.ProxyEnabled = $('#ProxyEnabled').checked;
            config.ProxyHost = $('#ProxyHost').value.trim();
            config.ProxyPort = parseInt($('#ProxyPort').value, 10) || 0;
            config.ProxyUsername = $('#ProxyUsername').value.trim();
            config.ProxyPassword = $('#ProxyPassword').value;

            const boxes = allBoxes();
            config.Rules = state.catalog.map((def) => {
                const mine = boxes.filter((b) => b.dataset.key === def.key);
                const text = state.messages[def.key] ?? '';
                return {
                    Key: def.key,
                    Enabled: mine.some((b) => b.dataset.col === 'server' && b.checked),
                    UserIds: mine.filter((b) => b.dataset.col !== 'server' && b.checked).map((b) => b.dataset.col),
                    // Store only customised texts, so default messages keep improving with updates.
                    Message: text.trim() === '' || text === def.defaultMessage ? '' : text
                };
            });
            config.UserConfigurations = [];

            const result = await ApiClient.updatePluginConfiguration(pluginUniqueId, config);
            state.config = config;
            Dashboard.processPluginConfigurationUpdateResult(result);
        } catch (err) {
            console.error('TelegramNotifier: failed to save configuration', err);
            Dashboard.alert('Salvataggio non riuscito.');
        } finally {
            Dashboard.hideLoadingMsg();
        }
    }

    async function sendTest() {
        const button = $('#TestButton');
        const result = $('#TestResult');
        button.disabled = true;
        result.textContent = 'Invio in corso...';
        try {
            const response = await ApiClient.fetch({
                type: 'POST',
                url: ApiClient.getUrl('TelegramNotifierApi/Test'),
                data: JSON.stringify({
                    BotToken: $('#BotToken').value.trim(),
                    ChatId: $('#ChatId').value.trim(),
                    ThreadId: $('#ThreadId').value.trim()
                }),
                contentType: 'application/json'
            });
            const text = await response.text();
            result.textContent = (response.ok ? '✅ ' : '❌ ') + text.replace(/^"|"$/g, '');
        } catch (err) {
            let text = 'Invio non riuscito: controlla i log di Jellyfin.';
            if (err && typeof err.text === 'function') {
                try { text = (await err.text()).replace(/^"|"$/g, ''); } catch (_) { /* keep default */ }
            }
            result.textContent = '❌ ' + text;
        } finally {
            button.disabled = false;
        }
    }

    $('#TelegramNotifierConfigForm').addEventListener('submit', save);
    $('#TestButton').addEventListener('click', sendTest);
    $('#TemplateKey').addEventListener('change', (e) => showTemplate(e.target.value));
    $('#TemplateReset').addEventListener('click', () => {
        const def = state.catalog.find((d) => d.key === state.currentTemplateKey);
        if (def) {
            $('#TemplateText').value = def.defaultMessage;
            storeCurrentTemplate();
        }
    });

    view.addEventListener('viewshow', load);
}
