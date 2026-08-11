export const assets = {
  lobby: "../assets/generated/quiz-lobby-night-v1-web.jpg",
  question: "../assets/generated/quiz-question-projector-v1-web.jpg",
  final: "../assets/generated/quiz-final-celebration-v1-web.jpg",
  organizer: "../assets/generated/quiz-organizer-table-v1-web.jpg",
  host: "../assets/generated/quiz-host-control-v1-web.jpg",
  public: "../assets/generated/quiz-public-room-v1-web.jpg",
  profile: "../assets/generated/quiz-profile-celebration-v1-web.jpg",
  stalkerFrame: "../assets/generated/quiz-question-stalker-frame-v1-web.jpg",
  plukFrame: "../assets/generated/quiz-question-pluk-frame-v1-web.jpg",
  entryQr: "../assets/generated/quiz-entry-qr-v1.svg"
};

export const gameInfo = {
  product: "РАУНД",
  id: "ИГРА-001",
  title: "Киновечер: крупный план",
  description: "Три раунда про советское и российское кино",
  template: "Кино 3×3",
  date: "27 июля 2026 года",
  startsAt: "19:00",
  place: "Бар «Смена», большой зал",
  roomCode: "QR-2048",
  teams: 10,
  players: 56,
  rounds: 3,
  questions: 9,
  timer: 15,
  organizer: "Анна Руднева",
  host: "Илья Соколов",
  scoring: "10 баллов за точный ответ",
  tieRule: "Выше команда с меньшим суммарным временем правильных ответов",
  correctReveal: "После завершения раунда",
  leaderboardReveal: "После каждого раунда"
};

export const roles = [
  { id: "player", label: "Игрок" },
  { id: "captain", label: "Капитан" },
  { id: "organizer", label: "Организатор" },
  { id: "host", label: "Ведущий" },
  { id: "public", label: "Экран зала" },
  { id: "overview", label: "Обзор" }
];

export const playerStates = [
  { id: "auth", label: "Вход" },
  { id: "team", label: "Команда" },
  { id: "lobby", label: "Сбор" },
  { id: "question", label: "Ответ" },
  { id: "submitted", label: "Ожидание" },
  { id: "reveal", label: "Разбор" },
  { id: "roundResults", label: "Таблица" },
  { id: "final", label: "Финал" },
  { id: "profile", label: "Профиль" }
];

export const organizerStates = [
  { id: "home", label: "Главная" },
  { id: "templates", label: "Шаблон" },
  { id: "editor", label: "Раунды" },
  { id: "rules", label: "Правила" },
  { id: "preview", label: "Просмотр" },
  { id: "review", label: "Проверка" },
  { id: "session", label: "Комната" },
  { id: "analytics", label: "Итоги" }
];

export const hostStates = [
  { id: "preflight", label: "Проверка" },
  { id: "round", label: "Раунд" },
  { id: "question", label: "Вопрос" },
  { id: "locked", label: "Закрыто" },
  { id: "reveal", label: "Разбор" },
  { id: "leaderboard", label: "Таблица" },
  { id: "final", label: "Финал" },
  { id: "report", label: "Отчёт" }
];

export const publicStates = [
  { id: "welcome", label: "QR" },
  { id: "lobby", label: "Сбор" },
  { id: "question", label: "Вопрос" },
  { id: "reveal", label: "Разбор" },
  { id: "leaderboard", label: "Таблица" },
  { id: "final", label: "Финал" }
];

export const user = {
  id: "maya",
  name: "Майя Волкова",
  shortName: "Майя",
  yandexId: "maya.volkova",
  ticket: "КИНО-7421",
  team: "Северный кадр",
  level: 7,
  xp: 1600,
  nextLevelXp: 2000,
  xpToNext: 400,
  finalXp: 180,
  finalScore: 80,
  correct: "8 из 9",
  finalPlace: "1 из 10"
};

export const teamMembers = [
  { id: "maya", name: "Майя Волкова", room: "В комнате", vote: "Майя", role: "Капитан", votes: 3 },
  { id: "lev", name: "Лев Астахов", room: "В комнате", vote: "Майя", role: "Участник", votes: 1 },
  { id: "nika", name: "Ника Белова", room: "В комнате", vote: "Майя", role: "Участник", votes: 1 },
  { id: "timur", name: "Тимур Сафин", room: "В комнате", vote: "Майя", role: "Участник", votes: 0 },
  { id: "olya", name: "Оля Корнеева", room: "В комнате", vote: "Лев", role: "Участник", votes: 0 },
  { id: "dima", name: "Дима Орлов", room: "Подключается последним", vote: "Ника", role: "Участник", votes: 0 }
];

export const teamRosters = {
  north: teamMembers,
  soft: [
    { id: "maya", name: "Майя Волкова", room: "В комнате", vote: "Майя", role: "Участник", votes: 1 },
    { id: "grisha-soft", name: "Гриша Нестеров", room: "В комнате", vote: "Гриша", role: "Участник", votes: 2 },
    { id: "lada-soft", name: "Лада Ким", room: "В комнате", vote: "Лада", role: "Участник", votes: 1 },
    { id: "roman-soft", name: "Роман Шаров", room: "В комнате", vote: "Гриша", role: "Участник", votes: 0 },
    { id: "inna-soft", name: "Инна Ефремова", room: "В комнате", vote: "Майя", role: "Участник", votes: 0 }
  ],
  "last-row": [
    { id: "maya", name: "Майя Волкова", room: "В комнате", vote: "Соня", role: "Участник", votes: 0 },
    { id: "sonya-last", name: "Соня Власова", room: "В комнате", vote: "Соня", role: "Участник", votes: 3 },
    { id: "gleb-last", name: "Глеб Матвеев", room: "В комнате", vote: "Соня", role: "Участник", votes: 1 },
    { id: "ira-last", name: "Ира Пак", room: "В комнате", vote: "Глеб", role: "Участник", votes: 1 },
    { id: "anton-last", name: "Антон Ларин", room: "В комнате", vote: "Соня", role: "Участник", votes: 0 },
    { id: "vera-last", name: "Вера Юдина", room: "В комнате", vote: "Соня", role: "Участник", votes: 0 }
  ]
};

export const availableTeams = [
  {
    id: "north",
    name: "Северный кадр",
    ticket: "КИНО-7421",
    detail: "5 из 6 участников уже в комнате",
    connected: 5,
    capacity: 6
  },
  {
    id: "soft",
    name: "Мягкий монтаж",
    ticket: "КИНО-7421",
    detail: "Все 5 участников уже в комнате",
    connected: 5,
    capacity: 5
  },
  {
    id: "last-row",
    name: "Последний ряд",
    ticket: "КИНО-7421",
    detail: "Все 6 участников уже в комнате",
    connected: 6,
    capacity: 6
  }
];

export const finalTeams = [
  { rank: 1, name: "Северный кадр", players: 6, correct: "8 из 9", score: 80, time: "70,4 с" },
  { rank: 2, name: "Мягкий монтаж", players: 5, correct: "8 из 9", score: 80, time: "76,9 с" },
  { rank: 3, name: "Последний ряд", players: 6, correct: "8 из 9", score: 80, time: "82,3 с" },
  { rank: 4, name: "Спойлеры", players: 6, correct: "7 из 9", score: 70, time: "63,0 с" },
  { rank: 5, name: "После титров", players: 6, correct: "7 из 9", score: 70, time: "71,8 с" },
  { rank: 6, name: "Пятый дубль", players: 6, correct: "6 из 9", score: 60, time: "58,3 с" },
  { rank: 7, name: "Крупный план", players: 5, correct: "6 из 9", score: 60, time: "61,5 с" },
  { rank: 8, name: "Плёнка 24", players: 5, correct: "5 из 9", score: 50, time: "52,1 с" },
  { rank: 9, name: "Тихая сцена", players: 5, correct: "5 из 9", score: 50, time: "56,7 с" },
  { rank: 10, name: "Зрительный зал", players: 6, correct: "3 из 9", score: 30, time: "35,7 с" }
];

export const roundLeaderboards = [
  {
    title: "Первый раунд — за вами",
    subtitle: "1 место · 30 баллов · 3 верных ответа",
    note: "До победы ещё два раунда.",
    teams: [
      { rank: 1, name: "Северный кадр", correct: "3 из 3", score: 30, time: "27,9 с" },
      { rank: 2, name: "Мягкий монтаж", correct: "3 из 3", score: 30, time: "31,4 с" },
      { rank: 3, name: "Последний ряд", correct: "2 из 3", score: 20, time: "15,8 с" },
      { rank: 4, name: "Крупный план", correct: "2 из 3", score: 20, time: "18,7 с" },
      { rank: 5, name: "Пятый дубль", correct: "2 из 3", score: 20, time: "22,0 с" },
      { rank: 6, name: "Спойлеры", correct: "2 из 3", score: 20, time: "23,6 с" },
      { rank: 7, name: "Плёнка 24", correct: "2 из 3", score: 20, time: "24,9 с" },
      { rank: 8, name: "Тихая сцена", correct: "2 из 3", score: 20, time: "26,4 с" },
      { rank: 9, name: "После титров", correct: "2 из 3", score: 20, time: "28,5 с" },
      { rank: 10, name: "Зрительный зал", correct: "1 из 3", score: 10, time: "8,6 с" }
    ]
  },
  {
    title: "Разрыв — один ответ",
    subtitle: "2 место · 50 баллов · 5 верных ответов",
    note: "Финальный раунд всё решит.",
    teams: [
      { rank: 1, name: "Мягкий монтаж", correct: "6 из 6", score: 60, time: "56,2 с" },
      { rank: 2, name: "Северный кадр", correct: "5 из 6", score: 50, time: "43,4 с" },
      { rank: 3, name: "Последний ряд", correct: "5 из 6", score: 50, time: "46,7 с" },
      { rank: 4, name: "Спойлеры", correct: "5 из 6", score: 50, time: "50,1 с" },
      { rank: 5, name: "После титров", correct: "5 из 6", score: 50, time: "54,8 с" },
      { rank: 6, name: "Крупный план", correct: "4 из 6", score: 40, time: "37,0 с" },
      { rank: 7, name: "Пятый дубль", correct: "4 из 6", score: 40, time: "39,9 с" },
      { rank: 8, name: "Плёнка 24", correct: "4 из 6", score: 40, time: "42,6 с" },
      { rank: 9, name: "Тихая сцена", correct: "4 из 6", score: 40, time: "49,5 с" },
      { rank: 10, name: "Зрительный зал", correct: "2 из 6", score: 20, time: "17,4 с" }
    ]
  },
  {
    title: "«Северный кадр» побеждает",
    subtitle: "80 баллов · 8 верных ответов · лучшее время среди лидеров",
    note: "У трёх команд по 80 баллов. «Северный кадр» оказался быстрее на правильных ответах.",
    teams: finalTeams
  }
];

export const rounds = [
  {
    title: "Узнай фильм",
    theme: "Кадры и узнавание",
    imageQuestions: 1,
    questions: [
      {
        code: "Р1-В1",
        type: "single",
        label: "Один вариант · изображение",
        title: "Какой фильм спрятан в этом кадре?",
        prompt: "Трое мужчин идут по заросшей железнодорожной насыпи в туманном пейзаже.",
        image: assets.stalkerFrame,
        imageAlt: "Трое мужчин на заросшей железнодорожной насыпи.",
        answers: ["Солярис", "Сталкер", "Зеркало", "Ностальгия"],
        correct: "Сталкер",
        teamAnswer: "Сталкер",
        teamResult: "верно",
        time: "6,8 секунды",
        explanation: "Это «Сталкер» Андрея Тарковского. Герои едут в запретную Зону, где, по слухам, исполняются самые сокровенные желания.",
        stats: ["Ответили: 10 из 10", "Правильно: 7 из 10", "«Сталкер»: 7", "«Солярис»: 1", "«Зеркало»: 1", "«Ностальгия»: 1"],
        points: 10
      },
      {
        code: "Р1-В2",
        type: "multiple",
        label: "Несколько вариантов",
        title: "Какие два фильма снял Леонид Гайдай?",
        prompt: "Выберите два варианта.",
        answers: ["Бриллиантовая рука", "Кавказская пленница", "Мимино", "Служебный роман"],
        correct: ["Бриллиантовая рука", "Кавказская пленница"],
        teamAnswer: "«Бриллиантовая рука» и «Кавказская пленница»",
        teamResult: "верно",
        time: "9,4 секунды",
        explanation: "«Бриллиантовую руку» и «Кавказскую пленницу» снял Леонид Гайдай. «Мимино» — фильм Георгия Данелии, а «Служебный роман» снял Эльдар Рязанов.",
        stats: ["Ответили: 10 из 10", "Полностью правильно: 6 из 10", "Выбрали «Бриллиантовую руку»: 8", "Выбрали «Кавказскую пленницу»: 7"],
        points: 10,
        required: 2
      },
      {
        code: "Р1-В3",
        type: "text",
        label: "Короткий ответ",
        title: "Как зовут героя Сергея Бодрова в фильме «Брат»?",
        prompt: "Например: имя и фамилия",
        answers: [],
        correct: "Данила Багров",
        accepted: ["Данила", "Данила Багров", "Багров"],
        teamAnswer: "Данила Багров",
        teamResult: "верно",
        time: "11,7 секунды",
        explanation: "Главного героя зовут Данила Багров. Эту роль исполнил Сергей Бодров-младший.",
        stats: ["Ответили: 9 из 10", "Правильно: 8 из 10", "Частый неверный ответ: Виктор Багров", "Без ответа: 1 команда"],
        points: 10,
        placeholder: "Например: имя и фамилия"
      }
    ]
  },
  {
    title: "Голоса и режиссёры",
    theme: "Создатели, роли и названия",
    imageQuestions: 0,
    questions: [
      {
        code: "Р2-В1",
        type: "single",
        label: "Один вариант",
        title: "Кто озвучил Волка в мультсериале «Ну, погоди!»?",
        prompt: "Выберите один вариант.",
        answers: ["Евгений Евстигнеев", "Евгений Леонов", "Анатолий Папанов", "Андрей Миронов"],
        correct: "Анатолий Папанов",
        teamAnswer: "Анатолий Папанов",
        teamResult: "верно",
        time: "5,2 секунды",
        explanation: "Волка озвучил Анатолий Папанов, а голосом Зайца стала Клара Румянова.",
        stats: ["Ответили: 10 из 10", "Правильно: 9 из 10"],
        points: 10
      },
      {
        code: "Р2-В2",
        type: "multiple",
        label: "Несколько вариантов",
        title: "Какие два фильма снял Эльдар Рязанов?",
        prompt: "Выберите два варианта.",
        answers: ["Берегись автомобиля", "Служебный роман", "Мимино", "Любовь и голуби"],
        correct: ["Берегись автомобиля", "Служебный роман"],
        teamAnswer: "«Берегись автомобиля» и «Служебный роман»",
        teamResult: "верно",
        time: "10,3 секунды",
        explanation: "Оба фильма снял Эльдар Рязанов. «Мимино» поставил Георгий Данелия, а «Любовь и голуби» — Владимир Меньшов.",
        stats: ["Ответили: 10 из 10", "Полностью правильно: 5 из 10", "Выбрали «Берегись автомобиля»: 8", "Выбрали «Служебный роман»: 7"],
        points: 10,
        required: 2
      },
      {
        code: "Р2-В3",
        type: "text",
        label: "Короткий ответ",
        title: "Закончите название фильма: «Иван Васильевич меняет…»",
        prompt: "Одно слово",
        answers: [],
        correct: "профессию",
        accepted: ["профессию", "свою профессию"],
        teamAnswer: "работу",
        teamResult: "неверно",
        time: "4,1 секунды",
        explanation: "Полное название — «Иван Васильевич меняет профессию». Комедия Леонида Гайдая вышла в 1973 году.",
        stats: ["Ответили: 10 из 10", "Правильно: 9 из 10", "Неверный ответ: работу"],
        points: 0,
        placeholder: "Одно слово"
      }
    ]
  },
  {
    title: "Финальные титры",
    theme: "Финальные вопросы и победа по времени",
    imageQuestions: 1,
    questions: [
      {
        code: "Р3-В1",
        type: "single",
        label: "Один вариант · изображение",
        title: "Из какого фильма этот кадр с планеты Плюк?",
        prompt: "Двое мужчин стоят среди песчаных холмов; один в длинном плаще, второй держит металлический предмет.",
        image: assets.plukFrame,
        imageAlt: "Двое мужчин среди песчаных холмов.",
        answers: ["Кин-дза-дза!", "Через тернии к звёздам", "Небеса обетованные", "Человек с бульвара Капуцинов"],
        correct: "Кин-дза-дза!",
        teamAnswer: "Кин-дза-дза!",
        teamResult: "верно",
        time: "8,0 секунды",
        explanation: "Плюк — пустынная планета из фильма Георгия Данелии «Кин-дза-дза!».",
        stats: ["Ответили: 10 из 10", "Правильно: 8 из 10"],
        points: 10
      },
      {
        code: "Р3-В2",
        type: "multiple",
        label: "Несколько вариантов",
        title: "Какие два фильма получили премию «Оскар» как лучший фильм на иностранном языке?",
        prompt: "Выберите два варианта.",
        answers: ["Москва слезам не верит", "Утомлённые солнцем", "Левиафан", "Движение вверх"],
        correct: ["Москва слезам не верит", "Утомлённые солнцем"],
        teamAnswer: "«Москва слезам не верит» и «Утомлённые солнцем»",
        teamResult: "верно",
        time: "12,6 секунды",
        explanation: "«Москва слезам не верит» получила награду в 1981 году, а «Утомлённые солнцем» — в 1995-м. «Левиафан» был номинирован, но не победил.",
        stats: ["Ответили: 9 из 10", "Полностью правильно: 4 из 10", "Это самый сложный вопрос вечера."],
        points: 10,
        required: 2
      },
      {
        code: "Р3-В3",
        type: "text",
        label: "Короткий ответ",
        title: "Назовите фамилию режиссёра фильма «Москва слезам не верит».",
        prompt: "Фамилия режиссёра",
        answers: [],
        correct: "Меньшов",
        accepted: ["Меньшов", "Владимир Меньшов", "Владимир Валентинович Меньшов"],
        teamAnswer: "Меньшов",
        teamResult: "верно",
        time: "6,4 секунды",
        explanation: "Фильм снял Владимир Меньшов. Картина вышла в 1980 году.",
        stats: ["Ответили: 10 из 10", "Правильно: 7 из 10"],
        points: 10,
        placeholder: "Фамилия режиссёра"
      }
    ]
  }
];

export const templates = [
  { id: "cinema", title: "Кино 3×3", subtitle: "Три раунда, девять вопросов, три типа ответа и готовый финал с таблицей команд.", state: "выбран" },
  { id: "music", title: "Музыкальный вечер", subtitle: "4 раунда · 12 вопросов", state: "лента" },
  { id: "team", title: "Знакомство с командой", subtitle: "3 раунда · 9 вопросов", state: "лента" },
  { id: "feedback", title: "Обратная связь после события", subtitle: "1 блок · 6 вопросов", state: "лента" }
];

export const achievements = [
  { title: "Первая игра", detail: "Начало положено", status: "Получено 28 июня", tone: "pulse" },
  { title: "Первая победа", detail: "Ваша первая победа", status: "Получено 27 июля", tone: "gold" },
  { title: "Без ошибки", detail: "3 из 3 в первом раунде", status: "Получено 27 июля", tone: "signal" },
  { title: "Серия из пяти", detail: "Пять точных ответов подряд", status: "Получено 27 июля", tone: "coral" },
  { title: "Эксперт: кино", detail: "20 верных ответов о кино", status: "Получено 27 июля", tone: "pulse" }
];

export const history = [
  { date: "27 июля 2026", title: "Киновечер: крупный план", place: "1 из 10", result: "80 баллов · 8 из 9", xp: "+180" },
  { date: "12 июля 2026", title: "Музыка на виниле", place: "3 из 12", result: "70 баллов · 7 из 10", xp: "+120" },
  { date: "28 июня 2026", title: "Городские истории", place: "5 из 9", result: "50 баллов · 5 из 8", xp: "+90" }
];

export const analytics = {
  current: ["10 из 10 команд в игре", "56 участников", "8 из 10 команд уже ответили", "До конца вопроса 9 секунд"],
  after: [
    "10 команд",
    "56 участников",
    "63 правильных ответа из 90",
    "70% правильных ответов",
    "8,6 секунды — среднее время ответа",
    "4,7 из 5 — общая оценка вечера",
    "48 из 56 участников оставили итоговую оценку"
  ],
  hardest: "Какие фильмы получили «Оскар»?",
  easiest: ["Кто озвучил Волка? · 9 из 10", "«Иван Васильевич меняет…» · 9 из 10"],
  roundRatings: ["Узнай фильм · 4,6 · 42 оценки", "Голоса и режиссёры · 4,4 · 39 оценок", "Финальные титры · 4,8 · 45 оценок"]
};

export const microcopy = {
  yandexButton: "Продолжить с Яндекс ID",
  demoNote: "Демонстрационный режим: данные остаются только в этом браузере.",
  authLoading: "Открываем Яндекс ID…",
  teamSearch: "Проверяем билет КИНО-7421…",
  firstQuestionWait: "Вопрос появится через несколько секунд",
  chooseAnswer: "Сначала выберите вариант",
  chooseTwo: "Выберите два варианта",
  enterAnswer: "Введите ответ",
  lockWarning: "После фиксации изменить ответ не получится.",
  submitted: "Ответ команды зафиксирован.",
  submittedLong: "Изменить ответ уже нельзя. Верный вариант появится после завершения раунда.",
  noCaptainAction: "Это действие доступно только капитану.",
  captainLocked: "Капитан уже закреплён.",
  ratingSaved: "Оценка сохранена.",
  xpAdded: "Опыт добавлен в профиль.",
  connectionRestored: "Связь восстановлена."
};
