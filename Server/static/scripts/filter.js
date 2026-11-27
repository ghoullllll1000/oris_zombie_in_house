(function () {
    function setSelectValue(selectId, paramName) {
        const params = new URLSearchParams(window.location.search);
        const value = params.get(paramName);
        const select = document.getElementById(selectId);
        if (!select || !value) return;
        const hasOption = Array.from(select.options).some(
            option => option.value === value
        );
        if (hasOption) select.value = value;
    }

    function restoreCharactersFilters() {
        setSelectValue('char-type', 'char-type');
        setSelectValue('ability', 'ability');
    }

    function restoreItemsFilters() {
        setSelectValue('item-type', 'item-type');
        setSelectValue('item-ability', 'item-ability');
    }

    function createCard(item) {
        const article = document.createElement('article');
        article.className = 'character-card';

        const avatar = document.createElement('div');
        avatar.className = 'character-avatar';

        const img = document.createElement('img');
        img.src = item.image;
        img.alt = item.name;
        avatar.appendChild(img);

        const info = document.createElement('div');
        info.className = 'character-info';

        const title = document.createElement('h3');
        title.textContent = item.name;

        const desc = document.createElement('p');
        desc.textContent = item.description;

        info.appendChild(title);
        info.appendChild(desc);

        article.appendChild(avatar);
        article.appendChild(info);

        return article;
    }

    function renderGrid(grid, items) {
        grid.innerHTML = '';

        if (items.length === 0) {
            const empty = document.createElement('p');
            empty.textContent = 'Ничего не найдено';
            grid.appendChild(empty);
            return;
        }

        items.forEach(function (item) {
            grid.appendChild(createCard(item));
        });
    }

    async function loadCharacters(form, grid) {
        const params = new URLSearchParams(new FormData(form));

        const response = await fetch(grid.dataset.endpoint + '?' + params.toString(), {
            headers: { 'Accept': 'application/json' }
        });

        if (!response.ok) {
            console.error('Filter request failed:', response.status);
            return;
        }

        const data = await response.json();

        const items = []
            .concat(data.characters || [])
            .concat(data.monsters || []);

        renderGrid(grid, items);
    }

    function bindCharacters() {
        const form = document.getElementById('characters-filter-form');
        const grid = document.getElementById('characters-grid');
        if (!form || !grid) return;

        form.addEventListener('submit', function (event) {
            event.preventDefault();
            loadCharacters(form, grid).catch(function (error) {
                console.error(error);
            });
        });

        loadCharacters(form, grid).catch(function (error) {
            console.error(error);
        });
    }

    restoreCharactersFilters();
    restoreItemsFilters();
    bindCharacters();
})();