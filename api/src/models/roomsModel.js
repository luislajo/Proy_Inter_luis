import { Schema, model } from "mongoose";

/**
 * @typedef roomSchema
 *
 * @property {import('mongoose').Types.ObjectId} _id
 * @property {string} type *
 * @property {string} roomNumber *
 * @property {number} maxGuests *
 * @property {string} description *
 * @property {string} mainImage *
 * @property {number} pricePerNight *
 *
 * @property {boolean} extraBed - extra bed (optional)
 * @property {boolean} crib - Cuna (optional)
 * @property {number} offer - discount (%) (optional)
 * @property {string[]} extras - Extras (list)
 * @property {string[]} extraImages - (list)
 *
 * @property {boolean} isAvailable - Default true
 * @property {number} rate - Default 0
 */
const roomDatabaseSchema = new Schema({
  type: { type: String, required: true, trim: true },

  roomNumber: { type: String, required: true, trim: true, unique: true },

  maxGuests: { type: Number, required: true, min: 1 },

  description: { type: String, required: true, trim: true },

  mainImage: { type: String, required: true, trim: true },

  pricePerNight: { type: Number, required: true, min: 0 },

  // ---- Optionals ----
  extraBed: { type: Boolean, default: false },
  crib: { type: Boolean, default: false },


  offer: { type: Number, default: 0, min: 0, max: 100 },


  extras: { type: [String], default: [] },
  extraImages: { type: [String], default: [] },

  // ---- Defaults ----
  status: {
    type: String,
    enum: ["available", "occupied", "cleaning", "maintenance", "blocked"],
    default: "available",
    required: true
  },
  isAvailable: { type: Boolean, default: true },
  rate: { type: Number, default: 0, min: 0, max: 5 },
});

roomDatabaseSchema.pre("validate", function () {
  if (this.status) {
    this.isAvailable = this.status === "available";
  }
});

export const roomDatabaseModel = model("room", roomDatabaseSchema);

/** Clase que obtiene los datos para la habitación */
class RoomEntryData {
  /**
   * @param {string} type
   * @param {string} roomNumber
   * @param {number} maxGuests
   * @param {string} description
   * @param {string} mainImage
   * @param {number} pricePerNight
   */
  constructor(type, roomNumber, maxGuests, description, mainImage, pricePerNight) {
    this.type = type;
    this.roomNumber = roomNumber;
    this.maxGuests = maxGuests;
    this.description = description;
    this.mainImage = mainImage;
    this.pricePerNight = pricePerNight;

    // defaults
    this.isAvailable = true;
    this.rate = 0;

    // opcionales
    this.extraBed = false;
    this.crib = false;
    this.offer = 0;
    this.extras = [];
    this.extraImages = [];

    this.ready = false;
  }

  /**
   * Completa los datos opcionales
   * @param {boolean} extraBed
   * @param {boolean} crib
   * @param {number} offer  // porcentaje 0-100
   * @param {string[]} extras
   * @param {string[]} extraImages
   */
  completeRoomData(extraBed = false, crib = false, offer = 0, extras = [], extraImages = []) {
    this.extraBed = extraBed;
    this.crib = crib;
    this.offer = offer;
    this.extras = extras;
    this.extraImages = extraImages;
    this.ready = true;
  }

  validate() {
    const isEmptyString = (v) => typeof v !== "string" || v.trim().length === 0;

    if (isEmptyString(this.type)) throw new Error("type vacío");
    if (isEmptyString(this.roomNumber)) throw new Error("roomNumber vacío");
    if (typeof this.maxGuests !== "number" || this.maxGuests < 1)
      throw new Error("maxGuests inválido");
    if (isEmptyString(this.description)) throw new Error("description vacía");
    if (isEmptyString(this.mainImage)) throw new Error("mainImage vacía");
    if (typeof this.pricePerNight !== "number" || this.pricePerNight < 0)
      throw new Error("pricePerNight inválido");

    if (!Array.isArray(this.extras)) this.extras = [];
    this.extras = this.extras.map(s => String(s).trim()).filter(Boolean);

    if (!Array.isArray(this.extraImages)) this.extraImages = [];
    this.extraImages = this.extraImages.map(s => String(s).trim()).filter(Boolean);

    if (typeof this.offer !== "number") this.offer = 0;
    this.offer = Math.max(0, Math.min(this.offer, 100));
  }

  /**
   * Establece la disponibilidad
   * @param {boolean} isAvailable
   */
setAvailability(isAvailable) {
  if (typeof isAvailable === "boolean") {
    this.isAvailable = isAvailable;
    return;
  }

}


  /**
   * @returns {import("mongoose").Document}
   */
  toDocument() {
    if (!this.ready) throw new Error("Habitación no lista. Completa la información");
    this.validate();
    return new roomDatabaseModel({
      type: this.type,
      roomNumber: this.roomNumber,
      maxGuests: this.maxGuests,
      description: this.description,
      mainImage: this.mainImage,
      pricePerNight: this.pricePerNight,

      extraBed: this.extraBed,
      crib: this.crib,
      offer: this.offer,
      extras: this.extras,
      extraImages: this.extraImages,

      isAvailable: this.isAvailable,
      rate: this.rate,
    });
  }
}

export { RoomEntryData };
