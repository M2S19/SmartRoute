import type { Depot, Route } from '../types';
import { mockDriver, mockOtherDrivers } from './users.mock';

const today = new Date().toISOString().slice(0, 10);

function todayAt(time: string): string {
  return new Date(`${today}T${time}:00`).toISOString();
}

export const depotStrasbourg: Depot = {
  id: 'dep-stg',
  name: 'Dépôt Strasbourg',
  address: { label: '2 rue du Port du Rhin, Strasbourg', latitude: 48.5734, longitude: 7.7931 },
};

export const depotMulhouse: Depot = {
  id: 'dep-mul',
  name: 'Dépôt Mulhouse',
  address: { label: "10 rue de l'Industrie, Mulhouse", latitude: 47.7508, longitude: 7.3359 },
};

/** Tournée du livreur connecté (maquette design/driver-route). */
export const mockDriverRoute: Route = {
  id: 'rt-stg-am',
  name: 'Tournée Strasbourg centre',
  date: today,
  status: 'IN_PROGRESS',
  depot: depotStrasbourg,
  driver: mockDriver,
  stops: [
    {
      id: 'stop-8801',
      order: 1,
      status: 'DELIVERED',
      deliveredAt: todayAt('08:42'),
      delivery: {
        id: 'dlv-8801',
        parcelRef: '8801',
        parcelType: 'STANDARD',
        address: { label: '12 rue Exemple, Strasbourg' },
      },
    },
    {
      id: 'stop-8802',
      order: 2,
      status: 'PENDING',
      delivery: {
        id: 'dlv-8802',
        parcelRef: '8802',
        parcelType: 'STANDARD',
        address: { label: '25 avenue des Vosges, Strasbourg' },
        instructions: 'Bâtiment B, Interphone 14',
      },
    },
    {
      id: 'stop-8803',
      order: 3,
      status: 'PENDING',
      delivery: {
        id: 'dlv-8803',
        parcelRef: '8803',
        parcelType: 'FRAGILE',
        address: { label: '8 rue de Colmar, Strasbourg' },
      },
    },
    {
      id: 'stop-8804',
      order: 4,
      status: 'PENDING',
      delivery: {
        id: 'dlv-8804',
        parcelRef: '8804',
        parcelType: 'STANDARD',
        address: { label: '17 rue du Rhin, Strasbourg' },
      },
    },
  ],
};

export const mockMulhouseRoute: Route = {
  id: 'rt-mul-am',
  name: 'Tournée Mulhouse matin',
  date: today,
  status: 'ASSIGNED',
  depot: depotMulhouse,
  driver: mockOtherDrivers[0],
  stops: [
    {
      id: 'stop-9101',
      order: 1,
      status: 'PENDING',
      delivery: { id: 'dlv-9101', parcelRef: '9101', parcelType: 'STANDARD', address: { label: '12 rue de Bâle, Mulhouse' } },
    },
    {
      id: 'stop-9102',
      order: 2,
      status: 'PENDING',
      delivery: { id: 'dlv-9102', parcelRef: '9102', parcelType: 'FRAGILE', address: { label: '5 avenue Kennedy, Mulhouse' } },
    },
    {
      id: 'stop-9103',
      order: 3,
      status: 'PENDING',
      delivery: { id: 'dlv-9103', parcelRef: '9103', parcelType: 'STANDARD', address: { label: '21 rue de Colmar, Mulhouse' } },
    },
    {
      id: 'stop-9104',
      order: 4,
      status: 'PENDING',
      delivery: { id: 'dlv-9104', parcelRef: '9104', parcelType: 'STANDARD', address: { label: '8 rue de Strasbourg, Mulhouse' } },
    },
  ],
};

export const mockColmarRoute: Route = {
  id: 'rt-col-pm',
  name: 'Tournée Colmar après-midi',
  date: today,
  status: 'ASSIGNED',
  depot: { id: 'dep-col', name: 'Dépôt Colmar', address: { label: '3 rue des Artisans, Colmar' } },
  driver: mockOtherDrivers[1],
  stops: [],
};

/** Tournées du jour candidates à l'affectation d'une nouvelle livraison. */
export const mockActiveRoutes: Route[] = [mockDriverRoute, mockMulhouseRoute, mockColmarRoute];
