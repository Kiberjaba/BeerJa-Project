export const productBrand = {
  name: "BeerJa",
  eyebrow: "Интерактивные события",
  promise: "Ведущий управляет моментом. Зал становится частью игры.",
  description: "Квизы, голосования и сценарные механики с входом по QR или коду комнаты — на телефонах участников и общем экране."
};

export const upcomingEvents = [
  { id: "cinema-aug", date: "17 августа", time: "19:30", title: "Киновечер: крупный план", place: "Бар «Смена»", status: "12 мест" },
  { id: "city-stories", date: "24 августа", time: "20:00", title: "Городские истории", place: "Дом культуры «Луч»", status: "регистрация" },
  { id: "blind-tasting", date: "6 сентября", time: "18:00", title: "Винный батл вслепую", place: "Секретная площадка", status: "анонс" }
];

export const mechanics = [
  {
    slug: "cinema-3x3",
    title: "Кино 3×3",
    kind: "Командный квиз",
    complexity: "Базовая",
    price: "от 24 000 ₽",
    description: "Три раунда, девять вопросов, капитан команды и общий финал с таблицей результатов.",
    image: "/assets/generated/quiz-question-projector-v1-web.jpg"
  },
  {
    slug: "audience-voice",
    title: "Мнение зала",
    kind: "Голосование",
    complexity: "Сценарная",
    price: "от 18 000 ₽",
    description: "Соберите выбор, рейтинг или короткие ответы аудитории и покажите результат в прямом эфире.",
    image: "/assets/generated/quiz-public-room-v1-web.jpg"
  },
  {
    slug: "custom-night",
    title: "Вечер под ключ",
    kind: "Авторский сценарий",
    complexity: "Под ключ",
    price: "от 49 000 ₽",
    description: "Механика, визуальный пакет, вопросы, ведущий и аналитика под конкретное событие.",
    image: "/assets/generated/quiz-final-celebration-v1-web.jpg"
  }
];

export const gallery = [
  { src: "/assets/generated/quiz-lobby-night-v1-web.jpg", alt: "Участники собираются перед началом игры", caption: "Команды входят по QR" },
  { src: "/assets/generated/quiz-host-control-v1-web.jpg", alt: "Ведущий управляет вопросами со смартфона", caption: "Ведущий держит темп" },
  { src: "/assets/generated/quiz-final-celebration-v1-web.jpg", alt: "Финальный экран интерактивной игры", caption: "Результат видит весь зал" }
];

export const roomFixtures = {
  "QR-2048": { gameId: "game-001", title: "Киновечер: крупный план", startsAt: "Сегодня, 19:00", place: "Бар «Смена»", status: "open" },
  "PULSE-9": { gameId: "game-009", title: "Мнение зала", startsAt: "Сегодня, 20:30", place: "Конференц-зал", status: "waiting" }
};

export const playerProfile = {
  id: 42,
  name: "Майя Волкова",
  email: "maya.volkova@yandex.ru",
  initials: "М",
  level: 7,
  xp: 1600,
  nextLevelXp: 2000,
  xpToNext: 400,
  totalScore: 3240,
  sessions: 12,
  team: "Северный кадр"
};

export const playerGames = [
  {
    id: "game-001",
    status: "completed",
    date: "27 июля 2026",
    title: "Киновечер: крупный план",
    place: "Бар «Смена»",
    roomCode: "QR-2048",
    team: "Северный кадр",
    rank: 1,
    teams: 10,
    score: 80,
    xp: 180,
    correct: 8,
    questions: 9,
    accuracy: 89,
    averageTime: "7,4 с",
    answers: [
      { round: "Узнай фильм", result: "3 из 3", score: 30, tone: "signal" },
      { round: "Голоса и режиссёры", result: "2 из 3", score: 20, tone: "cloud" },
      { round: "Финальные титры", result: "3 из 3", score: 30, tone: "gold" }
    ]
  },
  {
    id: "game-002",
    status: "upcoming",
    date: "17 августа, 19:30",
    title: "Городские истории",
    place: "Дом культуры «Луч»",
    roomCode: "CITY-17",
    team: "Северный кадр"
  },
  {
    id: "game-003",
    status: "completed",
    date: "28 июня 2026",
    title: "Музыкальный вечер",
    place: "Пространство «Контур»",
    roomCode: "NEON-LOOP",
    team: "Последний ряд",
    rank: 4,
    teams: 9,
    score: 55,
    xp: 90,
    correct: 6,
    questions: 9,
    accuracy: 67,
    averageTime: "9,8 с",
    answers: [
      { round: "Интро", result: "2 из 3", score: 20, tone: "cloud" },
      { round: "Припев", result: "3 из 3", score: 30, tone: "signal" },
      { round: "Финал", result: "1 из 3", score: 5, tone: "coral" }
    ]
  }
];

export const playerAchievements = [
  { title: "Первая победа", detail: "Занять первое место", state: "получено 27 июля" },
  { title: "Без подсказок", detail: "Три точных ответа подряд", state: "получено 27 июля" },
  { title: "Командный ритм", detail: "Провести пять игр одной командой", state: "4 из 5" },
  { title: "Быстрый кадр", detail: "Ответить правильно быстрее чем за 5 секунд", state: "получено 28 июня" }
];

export const hostProfile = {
  name: "Илья Соколов",
  email: "ilya@beerja.ru",
  initials: "ИС",
  completedGames: 18,
  participants: 764,
  rating: "4,8"
};

export const hostGames = [
  {
    id: "game-001",
    status: "ready",
    date: "Сегодня, 19:00",
    title: "Киновечер: крупный план",
    place: "Бар «Смена»",
    roomCode: "QR-2048",
    mechanic: "Кино 3×3",
    teams: 10,
    players: 56
  },
  {
    id: "game-004",
    status: "draft",
    date: "24 августа, 20:00",
    title: "Городские истории",
    place: "Дом культуры «Луч»",
    roomCode: "CITY-17",
    mechanic: "Вечер под ключ",
    readiness: 72
  },
  {
    id: "game-archive-1",
    status: "completed",
    date: "27 июля 2026",
    title: "Музыкальный вечер",
    place: "Пространство «Контур»",
    roomCode: "NEON-LOOP",
    mechanic: "Командный квиз",
    teams: 9,
    players: 48,
    answers: 356,
    accuracy: 68,
    rating: "4,7",
    hardest: "Узнать песню по барабанной партии"
  }
];

export const orderOptions = {
  eventTypes: ["Корпоратив", "Открытая игра", "Конференция", "Частное событие"],
  audiences: ["до 30", "30–80", "80–200", "больше 200"],
  budgets: ["до 25 000 ₽", "25–50 000 ₽", "50–100 000 ₽", "обсудить"]
};
