import { routeServiceHttp } from '../services/route/routeService.http';
import {
  saveRoutes,
  getRoutesByUser
} from './repositories/routeRepository';



export async function testDatabase(userId: string) {
  try {
    const localRoutes = await getRoutesByUser(userId);

    console.log('TEST HORS LIGNE');
    console.log('Nombre de tournées locales :', localRoutes.length);
    console.log('Tournées SQLite :', localRoutes);
  } catch (error) {
    console.error('Erreur lecture SQLite :', error);
  }
}