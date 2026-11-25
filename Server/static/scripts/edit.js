
(function () {
    const typeSelect = document.getElementById('type');
    const container = document.getElementById('dynamic-field-container');
    const label = document.getElementById('dynamic-field-label');
    const form = document.querySelector('.add-panel form');
    const description = document.getElementById('characteristics');
    const name = document.getElementById('name');

    const currentType = document.getElementById('current-type').value;
    const currentCategory = document.getElementById('current-category').value;

    const configs = {
        character: {
            label: 'Здоровье',
            name: 'health',
            values: ['3', '5']
        },
        monster: {
            label: 'Тип монстра',
            name: 'category',
            values: ['Обычный', 'Босс']
        },
        weapon: {
            label: 'Тип оружия',
            name: 'category',
            values: [
                'Холодное оружие',
                'Огнестрельное оружие',
                'Взрывчатка',
                'Специальное оружие'
            ]
        },
        item: {
            label: 'Тип вещи',
            name: 'category',
            values: ['Здоровье', 'Защита проёмов', 'Машина']
        }
    };

    function updateField() {
        const config = configs[typeSelect.value];

        const oldField = container.querySelector('select');
        if (oldField) {
            oldField.remove();
        }

        if (!config) return;

        label.textContent = config.label;

        const select = document.createElement('select');
        select.name = config.name;
        select.id = 'dynamic-field-input';
        select.required = true;

        config.values.forEach(function (value) {
            const option = document.createElement('option');
            option.value = value;
            option.textContent = value;
            select.appendChild(option);
        });

        if (typeSelect.value === currentType) {
            select.value = currentCategory;
        }

        container.appendChild(select);
    }

    typeSelect.value = currentType;
    updateField();

    typeSelect.addEventListener('change', updateField);

    form.addEventListener('submit', function (event) {
        const dynamicField = document.getElementById('dynamic-field-input');

        if (!dynamicField || !dynamicField.value) {
            event.preventDefault();
            alert('Заполните категорию или здоровье!');
            return;
        }

        if (!name.value.trim()) {
            event.preventDefault();
            alert('Введите имя!');
            return;
        }

        if (name.value.trim().length < 2 || name.value.trim().length > 100) {
            event.preventDefault();
            alert('Имя должно содержать от 2 до 100 символов!');
            return;
        }

        if (!/^[а-яА-ЯёЁa-zA-Z0-9 -]+$/.test(name.value.trim())) {
            event.preventDefault();
            alert('Имя содержит недопустимые символы!');
            return;
        }

        if (!description.value.trim()) {
            event.preventDefault();
            alert('Заполните описание!');
        }
    });
})();